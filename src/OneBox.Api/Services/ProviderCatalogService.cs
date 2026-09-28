namespace OneBox.Api.Services;

public sealed record ProviderDefinition(
    string Id,
    string Name,
    string[] TaskTypes,
    string StartUrl,
    string[] AllowedHosts,
    string Mode,
    bool RequiresUserLogin = true);

public sealed class ProviderCatalogService
{
    private readonly Dictionary<string, ProviderDefinition> _providers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["bookmyshow-india"] = new("bookmyshow-india", "BookMyShow India", ["MOVIE_BOOKING", "EVENT_BOOKING", "SHOPPING_RETURN"], "https://in.bookmyshow.com/", ["in.bookmyshow.com", "bookmyshow.com"], "browser"),
        ["irctc"] = new("irctc", "IRCTC", ["TRAVEL_BOOKING"], "https://www.irctc.co.in/", ["www.irctc.co.in", "irctc.co.in", "contents.irctc.co.in"], "browser"),
        ["google-maps"] = new("google-maps", "Google Maps / Places", ["HOSPITAL_APPOINTMENT", "RESTAURANT_BOOKING", "LOCAL_SERVICE"], "https://www.google.com/maps/", ["www.google.com", "google.com"], "discovery")
    };

    public IReadOnlyCollection<ProviderDefinition> All => _providers.Values;
    public ProviderDefinition? Get(string id) => _providers.TryGetValue(id, out var p) ? p : null;
    public ProviderDefinition? ForTask(string taskType) => _providers.Values.FirstOrDefault(p => p.TaskTypes.Contains(taskType, StringComparer.OrdinalIgnoreCase));
}
