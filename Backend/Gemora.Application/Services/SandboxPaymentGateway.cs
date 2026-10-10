using Gemora.Application.Interfaces;

namespace Gemora.Application.Services;

/// <summary>
/// Local payment gateway used until a real provider SDK is configured.
/// It accepts only opaque browser-created demo tokens and never receives
/// or stores raw card details.
/// </summary>
public sealed class SandboxPaymentGateway : IPaymentGateway
{
    public const string ProviderName = "GemoraSandbox";
    public const string LegacyPaymentMethodToken = "demo_card_legacy";

    public Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid orderId,
        decimal amount,
        string currency)
    {
        var intent = new PaymentIntentResult(
            PaymentIntentId: $"gemora_pi_{Guid.NewGuid():N}",
            Provider: ProviderName,
            Amount: amount,
            Currency: currency,
            ExpiresAt: DateTime.UtcNow.AddMinutes(30));

        return Task.FromResult(intent);
    }

    public Task<PaymentConfirmationResult> ConfirmPaymentAsync(
        string paymentIntentId,
        string paymentMethodToken,
        decimal amount,
        string currency)
    {
        if (!paymentIntentId.StartsWith("gemora_pi_", StringComparison.Ordinal))
        {
            return Task.FromResult(new PaymentConfirmationResult(
                false,
                paymentIntentId,
                ProviderName,
                "The payment intent is invalid."));
        }

        if (!paymentMethodToken.StartsWith("demo_card_", StringComparison.Ordinal))
        {
            return Task.FromResult(new PaymentConfirmationResult(
                false,
                paymentIntentId,
                ProviderName,
                "The card token is invalid or expired."));
        }

        if (paymentMethodToken.EndsWith("_decline", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PaymentConfirmationResult(
                false,
                paymentIntentId,
                ProviderName,
                "The card issuer declined this payment."));
        }

        return Task.FromResult(new PaymentConfirmationResult(
            true,
            paymentIntentId,
            ProviderName));
    }
}
