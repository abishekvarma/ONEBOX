using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneBox.Api.Data;
using OneBox.Api.Services;

namespace OneBox.Api.Controllers;

[Authorize,ApiController,Route("api/scheduled-tasks")]
public sealed class ScheduledTasksController(OneBoxDb db,ScheduledTaskService service):ControllerBase
{
    Guid UserId=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await db.ScheduledTasks.Where(x=>x.UserId==UserId).OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(ScheduledTaskRequest request,CancellationToken ct)
    {
        try{return Ok(await service.CreateAsync(UserId,request,ct));}
        catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}
    }

    [HttpPost("{id:guid}/observation")]
    public async Task<IActionResult> Observation(Guid id,[FromBody] JsonElement condition,CancellationToken ct)
    {
        try{await service.UpdateObservationAsync(UserId,id,condition.GetRawText(),ct);return Ok(new{status="UPDATED"});}
        catch(KeyNotFoundException ex){return NotFound(new{message=ex.Message});}
        catch(InvalidOperationException ex){return Conflict(new{message=ex.Message});}
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id,CancellationToken ct)
    {
        var task=await db.ScheduledTasks.SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==UserId,ct);
        if(task is null)return NotFound();
        if(task.Status is "COMPLETED" or "CANCELLED")return Conflict(new{message="Scheduled task is no longer active."});
        task.Status="CANCELLED";task.UpdatedAt=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(task);
    }
}
