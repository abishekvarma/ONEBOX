using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneBox.Api.Services;
namespace OneBox.Api.Controllers;
[Authorize,ApiController,Route("api/languages")]
public sealed class LanguageController:ControllerBase
{
 [HttpGet]public IActionResult Get()=>Ok(new{languages=LanguageService.All});
 [HttpGet("detect")]public IActionResult Detect([FromQuery]string text)=>Ok(new{language=LanguageService.Get(LanguageService.Detect(text??""))});
}