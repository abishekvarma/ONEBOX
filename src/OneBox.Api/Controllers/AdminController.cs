using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using OneBox.Api.Data;
namespace OneBox.Api.Controllers;
[Authorize(Roles="ADMIN"),ApiController,Route("api/admin")]
public sealed class AdminController(OneBoxDb db):ControllerBase{
 [HttpGet("overview")]public async Task<IActionResult> Overview(CancellationToken ct){var total=await db.Tasks.CountAsync(ct);var completed=await db.Tasks.CountAsync(x=>x.Status=="COMPLETED",ct);var bookings=await db.Bookings.CountAsync(ct);var users=await db.Users.CountAsync(ct);var recovery=await db.Tasks.CountAsync(x=>x.Status=="RECOVERY_REQUIRED",ct);return Ok(new{users,tasks=total,completed,bookings,recoveryRequired=recovery,successRate=total==0?0:Math.Round(completed*100.0/total,2)});}
}
