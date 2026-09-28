using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using OneBox.Api.Data;
namespace OneBox.Api.Controllers;
[Authorize,ApiController,Route("api/provider-health")]
public sealed class ProviderHealthController(OneBoxDb db):ControllerBase{
 [HttpGet]public async Task<IActionResult> All(CancellationToken ct){var rows=await db.ProviderHealth.OrderBy(x=>x.Provider).ToListAsync(ct);return Ok(rows);}
}
