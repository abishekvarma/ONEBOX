using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneBox.Api.Data;
using OneBox.Api.Services;

namespace OneBox.Api.Controllers;

[Authorize, ApiController, Route("api/execution")]
public sealed class ExecutionController(OneBoxDb db) : ControllerBase
{
    [HttpPost("session")]
    public IActionResult Session()
    {
        var secret = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Execution:AgentSharedSecret"];
        if (string.IsNullOrWhiteSpace(secret)) return Problem("Execution agent secret is not configured.");
        var exp = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");
        var payload = $"{exp}.{nonce}";
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return Ok(new { sessionToken = payload + "." + sig, expiresInSeconds = 300 });
    }

    [HttpGet("task/{taskId:guid}")]
    public async Task<IActionResult> Task(Guid taskId, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var task = await db.Tasks.SingleOrDefaultAsync(x => x.Id == taskId && x.UserId == userId, ct);
        if (task is null) return NotFound();
        var plan = JsonSerializer.Deserialize<AgentPlan>(task.PayloadJson);
        return plan is null ? BadRequest() : Ok(new { task.Id, task.Title, task.Type, task.Status, goal = plan.Details });
    }

    [HttpPost("result")]
    public async Task<IActionResult> Result(BrowserResultRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var task = await db.Tasks.Include(x => x.Steps).SingleOrDefaultAsync(x => x.Id == request.TaskId && x.UserId == userId, ct);
        if (task is null) return NotFound();
        if (task.Status == "COMPLETED") return Ok(new { task.Id, task.Status, idempotent = true });
        task.ResultJson = JsonSerializer.Serialize(new ConnectorResult(request.Success, request.Status, request.Message, request.ProviderReference, request.ProviderUrl, null, request.Amount));
        task.Status = request.Success ? "COMPLETED" : request.Status == "PAYMENT_SUCCESS_BOOKING_FAILED" ? "RECOVERY_REQUIRED" : "FAILED";
        var step = task.Steps.FirstOrDefault(x => x.StepOrder == 7);
        if (step is not null) { step.Status = request.Success ? "COMPLETED" : "FAILED"; step.Detail = request.Message; step.CompletedAt = DateTime.UtcNow; }
        db.AuditEvents.Add(new OneBox.Api.Models.AuditEvent { UserId = userId, TaskId = task.Id, EventType = request.Success ? "BROWSER_EXECUTION_COMPLETED" : "BROWSER_EXECUTION_FAILED", Detail = request.Message });
        if (request.Success && !string.IsNullOrWhiteSpace(request.ProviderReference))
            db.Bookings.Add(new OneBox.Api.Models.Booking { UserId = userId, TaskId = task.Id, Provider = request.Provider ?? "browser", Category = task.Type, ProviderReference = request.ProviderReference, ScheduledAt = request.ScheduledAt ?? DateTime.UtcNow, Amount = request.Amount, Status = "CONFIRMED", ProviderUrl = request.ProviderUrl });
        await db.SaveChangesAsync(ct);
        return Ok(new { task.Id, task.Status, request.ProviderReference, request.ProviderUrl });
    }
}

public sealed record BrowserResultRequest(Guid TaskId,bool Success,string Status,string Message,string? ProviderReference,string? ProviderUrl,decimal Amount=0,DateTime? ScheduledAt=null,string? Provider=null);

