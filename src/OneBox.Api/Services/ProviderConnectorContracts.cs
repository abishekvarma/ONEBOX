using OneBox.Api.Models;

namespace OneBox.Api.Services;

public sealed record ProviderConnectorInfo(
    string Id,
    string Name,
    string Category,
    string StartUrl,
    IReadOnlyList<string> SupportedActions,
    bool RequiresUserFinalConfirmation,
    bool SupportsBrowserHandoff);

public interface IProviderConnector
{
    ProviderConnectorInfo Info { get; }
    bool CanHandle(string taskType);
    Task<ProviderConnectorPreparation> PrepareAsync(OneTask task, string? requestedProvider, CancellationToken ct);
}

public sealed record ProviderConnectorPreparation(
    bool Success,
    string Status,
    string Message,
    string ProviderId,
    string ProviderName,
    string StartUrl,
    string TaskType,
    bool RequiresUserFinalConfirmation,
    IReadOnlyList<string> SupportedActions);

public sealed class ProviderConnectorRegistry(IEnumerable<IProviderConnector> connectors)
{
    private readonly IReadOnlyList<IProviderConnector> connectors = connectors.ToList();

    public IReadOnlyList<ProviderConnectorInfo> List(string? category = null) =>
        connectors.Select(x => x.Info)
            .Where(x => string.IsNullOrWhiteSpace(category) || x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public IProviderConnector? Resolve(string taskType, string? providerId = null)
    {
        return connectors.FirstOrDefault(x =>
            x.CanHandle(taskType) &&
            (string.IsNullOrWhiteSpace(providerId) || x.Info.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase)));
    }
}