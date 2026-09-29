using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneBox.Api.Data;
using OneBox.Api.Services;

namespace OneBox.Api.Controllers;

[Authorize, ApiController, Route("api/connectors")]
public sealed class ConnectorsController(
    ProviderConnectorRegistry registry,
    OneBoxDb db) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] string? category = null) =>
        Ok(registry.List(category));

    [HttpPost("prepare/{taskId:guid}")]
    public async Task<IActionResult> Prepare(Guid taskId, [FromBody] PrepareConnectorRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var task = await db.Tasks.SingleOrDefaultAsync(x => x.Id == taskId && x.UserId == userId, ct);
        if (task is null) return NotFound();

        var connector = registry.Resolve(task.Type, request.ProviderId);
        if (connector is null)
            return BadRequest(new { status = "NO_CONNECTOR", message = $"No provider connector is registered for {task.Type}." });

        var result = await connector.PrepareAsync(task, request.ProviderId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public sealed record PrepareConnectorRequest(string? ProviderId = null);