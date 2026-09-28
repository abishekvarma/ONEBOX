using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OneBox.Api.Data;
using OneBox.Api.Models;

namespace OneBox.Api.Services;

public sealed class AgentService(OneBoxDb db, IHttpClientFactory clients, IConfiguration cfg, GooglePlacesService places, ConnectorRegistry connectors, MovieDiscoveryService movies)
{
    public async Task<AgentResponse> RunAsync(Guid userId, string message, bool confirm, CancellationToken ct)
    {
        var plan=await PlanAsync(message,ct);
        var task=await CreateTaskAsync(userId,plan,ct);
        if(!string.IsNullOrWhiteSpace(plan.MissingInput)){task.Status="AWAITING_INPUT";await db.SaveChangesAsync(ct);return new(task.Id,task.Status,false,plan.MissingInput,plan,null);}\n        if(plan.Type=="MOVIE_BOOKING"){var title=ExtractMovieTitle(plan.Details);var options=await movies.SearchAsync(title,ct);if(options.Count>0){task.Status="OPTIONS_READY";task.ResultJson=JsonSerializer.Serialize(options);await db.SaveChangesAsync(ct);return new(task.Id,task.Status,false,$"I found {options.Count} movie matches. Choose the movie you want, then I’ll find nearby cinemas and showtimes.",plan,options);} }\n        if(plan.NeedsConfirmation && !confirm){task.Status="AWAITING_CONFIRMATION";await db.SaveChangesAsync(ct);return new(task.Id,task.Status,true,"I prepared the task. Review the exact provider, amount and action, then confirm. No irreversible action has happened.",plan,null);}
        return await ConfirmAsync(userId,task.Id,ct);
    }

    public async Task<AgentResponse> ConfirmAsync(Guid userId,Guid taskId,CancellationToken ct)
    {
        var user=await db.Users.FindAsync([userId],ct)??throw new InvalidOperationException("User not found.");
        var task=await db.Tasks.Include(x=>x.Steps).SingleOrDefaultAsync(x=>x.Id==taskId&&x.UserId==userId,ct)??throw new InvalidOperationException("Task not found.");
        if(task.Status=="COMPLETED") return ResponseFromTask(task);
        if(task.Status!="AWAITING_CONFIRMATION"&&task.Status!="READY") return new(task.Id,task.Status,false,"This task is not waiting for confirmation.",JsonSerializer.Deserialize<AgentPlan>(task.PayloadJson)!,DeserializeResult(task.ResultJson));
        task.AttemptCount++;task.Status="EXECUTING";await SetStep(task,6,"RUNNING","User confirmed the exact task; execution started.",ct);
        var plan=JsonSerializer.Deserialize<AgentPlan>(task.PayloadJson)!;var result=await ExecuteAsync(user,task,plan,ct);
        task.ResultJson=JsonSerializer.Serialize(result);task.Status=result.Success?"COMPLETED":result.Status switch{"PAYMENT_SUCCESS_BOOKING_FAILED"=>"RECOVERY_REQUIRED","ACTION_READY"=>"READY","BLOCKED"=>"BLOCKED",_=>"FAILED"};task.UpdatedAt=DateTime.UtcNow;
        if(result.Success && !string.IsNullOrWhiteSpace(result.ProviderReference))
            db.Bookings.Add(new Booking{UserId=userId,TaskId=task.Id,Provider=result.ProviderReference.StartsWith("ONEBOX-DEMO-",StringComparison.Ordinal)?"ONEBOX-SANDBOX":plan.Type,Category=plan.Type,ProviderReference=result.ProviderReference,ScheduledAt=result.ScheduledAt??DateTime.UtcNow,Amount=result.Amount,Status="CONFIRMED",ProviderUrl=result.ProviderUrl});
        await UpdateProviderHealth(plan.Type,result.Success,result.Message,ct);
        await db.SaveChangesAsync(ct);return new(task.Id,task.Status,false,result.Message,plan,result);
    }

    private AgentResponse ResponseFromTask(OneTask task){var p=JsonSerializer.Deserialize<AgentPlan>(task.PayloadJson)!;return new(task.Id,task.Status,false,"This task was already completed. ONEBOX did not execute it twice.",p,DeserializeResult(task.ResultJson));}
    private static object? DeserializeResult(string? json){if(string.IsNullOrWhiteSpace(json))return null;try{return JsonSerializer.Deserialize<ConnectorResult>(json);}catch{return null;}}

    private async Task<OneTask> CreateTaskAsync(Guid userId, AgentPlan p, CancellationToken ct)
    {
        var t = new OneTask { UserId = userId, Type = p.Type, Title = p.Title, RequiresConfirmation = p.NeedsConfirmation, PayloadJson = JsonSerializer.Serialize(p), Status = "PLANNED" };
        var names = new[] { "Understand request", "Plan task", "Find options", "Collect required details", "Request confirmation", "Execute action", "Verify result" };
        for (var i = 0; i < names.Length; i++) t.Steps.Add(new TaskStep { TaskId = t.Id, StepOrder = i + 1, Name = names[i], Status = i == 0 ? "COMPLETED" : "PENDING" });
        db.Tasks.Add(t); db.AuditEvents.Add(new AuditEvent { UserId = userId, TaskId = t.Id, EventType = "TASK_CREATED", Detail = p.Title });
        await db.SaveChangesAsync(ct); return t;
    }

    private async Task SetStep(OneTask t, int order, string status, string detail, CancellationToken ct)
    {
        var s = t.Steps.First(x => x.StepOrder == order); s.Status = status; s.Detail = detail; s.CompletedAt = status == "COMPLETED" ? DateTime.UtcNow : null; await db.SaveChangesAsync(ct);
    }
    private async Task UpdateProviderHealth(string provider,bool success,string? error,CancellationToken ct)
    {
        var h=await db.ProviderHealth.SingleOrDefaultAsync(x=>x.Provider==provider,ct);
        if(h is null){h=new ProviderHealth{Provider=provider};db.ProviderHealth.Add(h);}
        h.TotalAttempts++;if(success)h.SuccessfulAttempts++;h.SuccessRate=Math.Round((double)h.SuccessfulAttempts/h.TotalAttempts*100,2);h.Status=success?"HEALTHY":h.SuccessRate>=80?"DEGRADED":"UNHEALTHY";h.LastError=success?null:error;h.LastCheckedAt=DateTime.UtcNow;
    }

    private async Task<ConnectorResult> ExecuteAsync(AppUser user, OneTask task, AgentPlan p, CancellationToken ct)
    {
        if (p.Type is "HOSPITAL_APPOINTMENT" or "RESTAURANT_BOOKING")
        {
            if (user.Latitude is null || user.Longitude is null || !user.LocationAllowed)
                return new(false, "LOCATION_REQUIRED", "Allow location access before I search nearby providers.", null, null, null, 0);
            await SetStep(task, 3, "COMPLETED", "Live nearby provider search", ct);
            var opts = await places.SearchAsync(p.Type, user.Latitude.Value, user.Longitude.Value, ct);
            if (opts.Count == 0) return new(false, "NO_PROVIDERS", "No live providers were returned. Configure Google Places or a provider connector.", null, null, null, 0);
            await SetStep(task, 4, "COMPLETED", $"Found {opts.Count} live providers", ct);
            var serialized = JsonSerializer.Serialize(opts);
            task.ResultJson = serialized;
            await db.SaveChangesAsync(ct);
            return new(false, "ACTION_READY", $"Found {opts.Count} live providers. Select a provider and continue through its authorized booking flow.", null, null, null, 0);
        }

        if (connectors.TryGet(p.Type, out var connector) && connector is not null)
        {
            await SetStep(task, 6, "RUNNING", "Executing authorized connector", ct);
            var r=await connector.ExecuteAsync(user, task, JsonSerializer.SerializeToElement(p), ct);
            await SetStep(task,7,r.Success?"COMPLETED":"FAILED",r.Message,ct);
            return r;
        }

        if(string.Equals(cfg["Execution:Mode"],"sandbox",StringComparison.OrdinalIgnoreCase))
        {
            await SetStep(task,6,"RUNNING","Executing deterministic sandbox provider.",ct);
            var sandbox=new SandboxConnector(p.Type);
            var demo=await sandbox.ExecuteAsync(user,task,JsonSerializer.SerializeToElement(p),ct);
            await SetStep(task,7,demo.Success?"COMPLETED":"FAILED",demo.Message,ct);
            return demo;
        }
        return new(false,"CONNECTOR_REQUIRED","No authorized provider connector is configured for this workflow. ONEBOX stopped safely instead of fabricating a completion.",null,null,null,0);
    }

    private async Task<AgentPlan> PlanAsync(string message, CancellationToken ct)
    {
        var key = cfg["AI:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return Heuristic(message);
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, cfg["AI:BaseUrl"] ?? "https://api.openai.com/v1/responses");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            var schema = new { type = "json_schema", name = "onebox_task", strict = true, schema = new { type = "object", properties = new { type = new { type = "string", @enum = new[] { "HOSPITAL_APPOINTMENT", "RESTAURANT_BOOKING", "MOVIE_BOOKING", "TRAVEL_BOOKING", "PARCEL_BOOKING", "BILL_PAYMENT", "SHOPPING_RETURN", "GENERAL" } }, title = new { type = "string" }, needsConfirmation = new { type = "boolean" }, date = new { type = "string" }, time = new { type = "string" }, location = new { type = "string" }, details = new { type = "string" }, missingInput = new { type = "string" } }, required = new[] { "type", "title", "needsConfirmation", "date", "time", "location", "details", "missingInput" }, additionalProperties = false } };
            var body = new { model = cfg["AI:Model"] ?? "gpt-5.6-luna", store = false, instructions = "You are ONEBOX, a task-execution planner. Never claim an external action is complete unless the backend connector reports a provider reference. Payments, bookings, purchases, cancellations and submissions require confirmation.", input = message, text = new { format = schema } };
            req.Content = JsonContent.Create(body);
            using var res = await clients.CreateClient().SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return Heuristic(message);
            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(ct));
            var text = ExtractOutputText(doc.RootElement);
            return JsonSerializer.Deserialize<AgentPlan>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Heuristic(message);
        }
        catch { return Heuristic(message); }
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String) return ot.GetString()!;
        if (root.TryGetProperty("output", out var output)) foreach (var item in output.EnumerateArray()) if (item.TryGetProperty("content", out var content)) foreach (var c in content.EnumerateArray()) if (c.TryGetProperty("text", out var tx) && tx.ValueKind == JsonValueKind.String) return tx.GetString()!;
        return "{}";
    }

    private static string ExtractMovieTitle(string details){var m=System.Text.RegularExpressions.Regex.Match(details, @"\\b(?:for|called|named)\\s+(.+)$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);return m.Success?m.Groups[1].Value.Trim():details.Replace("book me a movie ticket","",StringComparison.OrdinalIgnoreCase).Trim();}\n\n    private static AgentPlan Heuristic(string m)
    {
        var s = m.ToLowerInvariant();
        if (s.Contains("movie") || s.Contains("cinema") || s.Contains("film")) { var hasTitle = System.Text.RegularExpressions.Regex.IsMatch(s, @"\\b(for|called|named)\\s+\\S+"); return new("MOVIE_BOOKING", "Movie ticket", true, "", "evening", "near me", m, hasTitle ? null : "Which movie would you like to watch?"); }\n        if (s.Contains("hospital") || s.Contains("doctor") || s.Contains("clinic")) return new("HOSPITAL_APPOINTMENT", "Hospital appointment", true, "", "evening", "near me", m);
        if (s.Contains("restaurant") || s.Contains("table") || s.Contains("dinner")) return new("RESTAURANT_BOOKING", "Restaurant booking", true, "", "", "near me", m);
        if (s.Contains("return") && s.Contains("order")) return new("SHOPPING_RETURN", "Return an order", true, "", "", "", m);
        if (s.Contains("bill") && s.Contains("pay")) return new("BILL_PAYMENT", "Pay a bill", true, "", "", "", m);
        if (s.Contains("parcel") || s.Contains("courier")) return new("PARCEL_BOOKING", "Send a parcel", true, "", "", "", m);
        if (s.Contains("book") && (s.Contains("flight") || s.Contains("hotel") || s.Contains("train"))) return new("TRAVEL_BOOKING", "Travel booking", true, "", "", "", m);
        return new("GENERAL", m, false, "", "", "", m);
    }
}
