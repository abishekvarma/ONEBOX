using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using OneBox.Api.Data;using OneBox.Api.Services;
namespace OneBox.Api.Controllers;
[Authorize,ApiController,Route("api/providers")]
public sealed class ProvidersController(OneBoxDb db,GooglePlacesService places):ControllerBase{Guid Id=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 [HttpGet("nearby")]public async Task<IActionResult> Nearby(string category,CancellationToken ct){var u=await db.Users.FindAsync([Id],ct);if(u is null)return NotFound();if(!u.LocationAllowed||u.Latitude is null||u.Longitude is null)return BadRequest(new{code="LOCATION_REQUIRED",message="Allow location access first."});var items=await places.SearchAsync(category,u.Latitude.Value,u.Longitude.Value,ct);return Ok(items);}
}
