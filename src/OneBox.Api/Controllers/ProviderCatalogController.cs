using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneBox.Api.Services;

namespace OneBox.Api.Controllers;

[Authorize, ApiController, Route("api/provider-catalog")]
public sealed class ProviderCatalogController(ProviderCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public IActionResult All() => Ok(catalog.All);

    [HttpGet("{id}")]
    public IActionResult Get(string id) => catalog.Get(id) is { } p ? Ok(p) : NotFound();

    [HttpGet("for-task/{taskType}")]
    public IActionResult ForTask(string taskType) => catalog.ForTask(taskType) is { } p ? Ok(p) : NotFound();
}
