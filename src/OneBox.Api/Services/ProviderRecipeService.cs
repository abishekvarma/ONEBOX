using System.Text.Json;
using OneBox.Api.Models;

namespace OneBox.Api.Services;

public sealed class ProviderRecipeService(IConfiguration cfg)
{
    public ExecutionPlan? Build(string taskType, string details, string? providerUrl)
    {
        if (string.IsNullOrWhiteSpace(providerUrl)) return null;
        if (!Uri.TryCreate(providerUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return null;

        // Recipes are deliberately data-driven. Production deployments should add provider-specific
        // selectors under ProviderRecipes rather than hard-coding them into the execution engine.
        var section = cfg.GetSection($"ProviderRecipes:{taskType}");
        var actions = new List<ExecutionAction>();
        foreach (var child in section.GetChildren().OrderBy(x => x.Key))
        {
            var kind = child["Kind"];
            if (string.IsNullOrWhiteSpace(kind)) continue;
            actions.Add(new ExecutionAction(kind, child["Selector"], child["Value"]));
        }
        return new ExecutionPlan(taskType, uri.ToString(), actions, true);
    }
}
