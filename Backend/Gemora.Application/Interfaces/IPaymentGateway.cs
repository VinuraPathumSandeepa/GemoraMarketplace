namespace Gemora.Application.Interfaces;

public interface IPaymentGateway
{
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid orderId,
        decimal amount,
        string currency);

    Task<PaymentConfirmationResult> ConfirmPaymentAsync(
        string paymentIntentId,
        string paymentMethodToken,
        decimal amount,
        string currency);
}

public sealed record PaymentIntentResult(
    string PaymentIntentId,
    string Provider,
    decimal Amount,
    string Currency,
    DateTime ExpiresAt);

public sealed record PaymentConfirmationResult(
    bool Succeeded,
    string PaymentIntentId,
    string Provider,
    string? FailureReason = null);
