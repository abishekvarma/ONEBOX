# Provider connector contract

A connector is the only component allowed to claim that an external action completed.

```csharp
public interface ITaskConnector
{
    string Type { get; }
    Task<ConnectorResult> ExecuteAsync(AppUser user, OneTask task, JsonElement payload, CancellationToken ct);
}
```

The connector must return `Success=true` only after the external provider returns a verifiable reference. Store that reference in `Booking.ProviderReference` and optionally the provider URL.

Examples:
- Hospital: authorized hospital/booking API
- Restaurant: authorized reservation provider API
- Travel: airline/OTA API
- Parcel: courier API
- Bill payment: authorized payment/biller API
- Shopping return: merchant/marketplace API or approved automation

If a provider has no authorized API or automation path, ONEBOX returns `NOT_CONFIGURED` and does not fabricate a result.
