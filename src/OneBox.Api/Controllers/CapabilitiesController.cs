using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneBox.Api.Services;

namespace OneBox.Api.Controllers;

[Authorize, ApiController, Route("api/capabilities")]
public sealed class CapabilitiesController(
    ProviderCatalogService providers,
    IConfiguration config) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var sandbox = string.Equals(config["Execution:Mode"], "sandbox", StringComparison.OrdinalIgnoreCase);
        var liveBrowserAgent = !string.IsNullOrWhiteSpace(config["ONEBOX_ALLOWED_HOSTS"] ?? config["BrowserAgent:AllowedHosts"]);
        var items = providers.All.Select(x => new
        {
            id = x.Id,
            name = x.Name,
            taskTypes = x.TaskTypes,
            mode = x.Mode,
            requiresUserLogin = x.RequiresUserLogin,
            ready = x.Mode == "discovery" || (x.Mode == "browser" && liveBrowserAgent),
            note = x.Mode == "discovery"
                ? "Discovery only; the provider controls the final booking."
                : liveBrowserAgent
                    ? "Provider session required; ONEBOX stops for provider authentication and irreversible confirmation."
                    : "Provider browser execution is not enabled in this deployment."
        });

        return Ok(new
        {
            executionMode = sandbox ? "sandbox" : "live",
            sandbox,
            liveBrowserExecutionConfigured = liveBrowserAgent,
            providers = items,
            payment = new
            {
                authentication = "provider",
                oneboxStoresPaymentPins = false,
                finalUserConfirmationRequired = true
            }
        });
    }
}
