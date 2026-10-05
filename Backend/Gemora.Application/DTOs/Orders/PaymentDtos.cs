namespace Gemora.Application.DTOs.Orders;

public class CreatePaymentRequestDto
{
    public string PaymentMethod { get; set; }
        = "Card";
}


public class PaymentResponseDto
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Provider { get; set; }
        = string.Empty;

    public string ExternalReference { get; set; }
        = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; }
        = string.Empty;

    public string Status { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderResponseDto Order { get; set; }
        = null!;
}