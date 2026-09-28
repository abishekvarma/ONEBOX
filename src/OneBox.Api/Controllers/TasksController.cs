using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using OneBox.Api.Data;using OneBox.Api.Services;
namespace OneBox.Api.Controllers;
[Authorize,ApiController,Route("api/tasks")]
public sealed class TasksController(OneBoxDb db,AgentService agent):ControllerBase{Guid Id=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet]public async Task<IActionResult> List(CancellationToken ct){var t=await db.Tasks.Where(x=>x.UserId==Id).OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct);return Ok(t);}
 [HttpGet("{taskId:guid}")]public async Task<IActionResult> Get(Guid taskId,CancellationToken ct){var t=await db.Tasks.Include(x=>x.Steps).SingleOrDefaultAsync(x=>x.Id==taskId&&x.UserId==Id,ct);return t is null?NotFound():Ok(t);}
 [HttpPost("agent")]public async Task<IActionResult> Agent(AgentRequest r,CancellationToken ct){if(string.IsNullOrWhiteSpace(r.Message))return BadRequest(new{message="Tell ONEBOX what you want done."});return Ok(await agent.RunAsync(Id,r.Message,false,ct));}
 [HttpPost("{taskId:guid}/confirm")]public async Task<IActionResult> Confirm(Guid taskId,CancellationToken ct)=>Ok(await agent.ConfirmAsync(Id,taskId,ct));
}
public record AgentRequest(string Message,bool Confirm=false);
