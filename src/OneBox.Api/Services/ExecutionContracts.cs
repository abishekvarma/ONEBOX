using System.Text.Json;
using OneBox.Api.Models;

namespace OneBox.Api.Services;

public record ExecutionAction(string Kind, string? Selector = null, string? Value = null, int? TimeoutMs = null);
public record ExecutionPlan(string Provider, string StartUrl, List<ExecutionAction> Actions, bool RequiresFinalConfirmation = true);
public record BrowserExecutionRequest(Guid TaskId, ExecutionPlan Plan, string SessionToken);
public record BrowserExecutionResult(bool Success, string Status, string Message, string? ProviderReference, string? ProviderUrl, Dictionary<string,string>? Data);

public interface IExecutionGateway
{
    Task<BrowserExecutionResult> ExecuteAsync(BrowserExecutionRequest request, CancellationToken ct);
}

public sealed class LocalExecutionGateway(IHttpClientFactory clients, IConfiguration cfg) : IExecutionGateway
{
    public async Task<BrowserExecutionResult> ExecuteAsync(BrowserExecutionRequest request, CancellationToken ct)
    {
        var baseUrl = cfg["Execution:LocalAgentUrl"] ?? "http://127.0.0.1:8787";
        using var http = clients.CreateClient();
        http.Timeout = TimeSpan.FromMinutes(5);
        using var msg = new HttpRequestMessage(HttpMethod.Post, baseUrl.TrimEnd('/') + "/execute");
        msg.Headers.Add("X-ONEBOX-SESSION", request.SessionToken);
        msg.Content = JsonContent.Create(request.Plan);
        using var res = await http.SendAsync(msg, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            return new(false, "EXECUTOR_ERROR", $"Local execution agent returned {(int)res.StatusCode}: {body}", null, null, null);
        return JsonSerializer.Deserialize<BrowserExecutionResult>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new(false, "INVALID_EXECUTOR_RESPONSE", "The execution agent returned an invalid response.", null, null, null);
    }
}
