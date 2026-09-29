using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneBox.Api.Services;
namespace OneBox.Api.Controllers;
[Authorize,ApiController,Route("api/commerce")]
public sealed class CommerceController(CommerceCatalogService catalog):ControllerBase
{
 [HttpGet("compare")]
 public IActionResult Compare([FromQuery]string query)
 {
  if(string.IsNullOrWhiteSpace(query))return BadRequest(new{message="Enter a product or model to compare."});
  var items=catalog.Search(query);
  var groups=items.GroupBy(x=>x.ProductId).Select(g=>new{productId=g.Key,title=g.First().Title,offers=g.OrderBy(x=>x.Price).ToList()}).ToList();
  return Ok(new{query,generatedAtUtc=DateTime.UtcNow,products=groups});
 }
}