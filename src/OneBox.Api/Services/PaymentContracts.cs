namespace OneBox.Api.Services;

public sealed record CheckoutQuote(decimal ProviderAmount, decimal PlatformFee, decimal Total, string Currency = "INR");
public sealed record PaymentSession(string Provider, string Status, string? CheckoutUrl, string? UpiIntentUri, CheckoutQuote Quote, string Message);

public interface IPaymentOrchestrator
{
    CheckoutQuote Quote(decimal providerAmount, decimal platformFee);
    PaymentSession CreateExternalCheckout(string provider, decimal providerAmount, decimal platformFee, string? returnUrl);
}

public sealed class PaymentOrchestrator(IConfiguration cfg) : IPaymentOrchestrator
{
    public CheckoutQuote Quote(decimal providerAmount, decimal platformFee) { if(providerAmount<0||platformFee<0) throw new ArgumentOutOfRangeException(); return new(providerAmount, platformFee, decimal.Round(providerAmount+platformFee,2,MidpointRounding.AwayFromZero)); }

    public PaymentSession CreateExternalCheckout(string provider, decimal providerAmount, decimal platformFee, string? returnUrl)
    {
        var quote = Quote(providerAmount, platformFee);
        // ONEBOX never asks for a customer's bank credentials or UPI PIN. A real merchant
        // checkout is supplied by an authorized payment gateway account owned by ONEBOX.
        // The gateway can expose UPI intent/QR and return the customer to returnUrl.
        var checkoutUrl = cfg[$"Payments:{provider}:CheckoutUrl"];
        if (string.IsNullOrWhiteSpace(checkoutUrl))
            return new(provider, "NOT_CONFIGURED", null, null, quote,
                "Configure an authorized payment-gateway checkout URL. ONEBOX does not collect bank credentials itself.");

        return new(provider, "REDIRECT_READY", checkoutUrl, null, quote,
            "Open the authorized payment gateway checkout. The payment provider handles UPI authentication.");
    }
}
