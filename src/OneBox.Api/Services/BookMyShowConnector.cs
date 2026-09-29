using OneBox.Api.Models;

namespace OneBox.Api.Services;

public sealed class BookMyShowConnector : IProviderConnector
{
    public ProviderConnectorInfo Info { get; } = new(
        "bookmyshow",
        "BookMyShow",
        "MOVIE_BOOKING",
        "https://in.bookmyshow.com/",
        new[] { "SEARCH_MOVIE", "SELECT_CINEMA", "SELECT_SHOW", "PREPARE_BOOKING" },
        true,
        true);

    public bool CanHandle(string taskType) =>
        taskType.Equals("MOVIE_BOOKING", StringComparison.OrdinalIgnoreCase);

    public Task<ProviderConnectorPreparation> PrepareAsync(
        OneTask task,
        string? requestedProvider,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requestedProvider) &&
            !Info.Id.Equals(requestedProvider, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ProviderConnectorPreparation(
                false, "PROVIDER_NOT_SUPPORTED",
                "The requested provider is not this connector.",
                Info.Id, Info.Name, Info.StartUrl, task.Type,
                Info.RequiresUserFinalConfirmation, Info.SupportedActions));
        }

        return Task.FromResult(new ProviderConnectorPreparation(
            true, "READY",
            "BookMyShow connector is ready to prepare the movie flow. ONEBOX will stop before the final booking/payment action and return control to the user.",
            Info.Id, Info.Name, Info.StartUrl, task.Type,
            Info.RequiresUserFinalConfirmation, Info.SupportedActions));
    }
}