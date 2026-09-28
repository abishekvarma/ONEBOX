using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using OneBox.Api.Data;
namespace OneBox.Api.Controllers;
[Authorize,ApiController,Route("api/me")]
public sealed class MeController(OneBoxDb db):ControllerBase{Guid Id=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet]public async Task<IActionResult> Get(CancellationToken ct){var u=await db.Users.FindAsync([Id],ct);return u is null?NotFound():Ok(new{u.Id,u.Name,u.Email,u.Phone,u.Latitude,u.Longitude,u.LocationAllowed});}
 [HttpPut("location")]public async Task<IActionResult> Location(LocationRequest r,CancellationToken ct){if(r.Latitude is < -90 or > 90||r.Longitude is < -180 or > 180)return BadRequest();var u=await db.Users.FindAsync([Id],ct);if(u is null)return NotFound();u.Latitude=r.Latitude;u.Longitude=r.Longitude;u.LocationAllowed=r.Allowed;await db.SaveChangesAsync(ct);return Ok(new{u.Latitude,u.Longitude,u.LocationAllowed});}
}
public record LocationRequest(double Latitude,double Longitude,bool Allowed);

