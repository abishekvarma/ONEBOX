using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneBox.Api.Services;

namespace OneBox.Api.Controllers;

[Authorize,ApiController,Route("api/commerce")]
public sealed class CommerceController(CommerceCatalogService catalog):ControllerBase
{
    [HttpGet("compare")]
    public async Task<IActionResult> Compare([FromQuery]string query,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(query)) return BadRequest(new{message="Enter a product or model to compare."});
        var items=await catalog.SearchAsync(query,ct);
        if(items.Count==0)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,new{message="No authorized live commerce provider returned offers. Configure the provider APIs before showing prices.",live=false});
        var groups=items.GroupBy(x=>x.ProductId).Select(g=>new{productId=g.Key,title=g.First().Title,offers=g.OrderBy(x=>x.Price).ToList()}).ToList();
        return Ok(new{query,generatedAtUtc=DateTime.UtcNow,live=true,products=groups});
    }
}