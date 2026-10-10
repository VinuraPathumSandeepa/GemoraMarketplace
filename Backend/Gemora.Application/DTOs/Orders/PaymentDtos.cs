using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.Orders;

public class CreatePaymentIntentRequestDto
{
    [Required]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Card";
}

public class ConfirmPaymentRequestDto
{
    [Required]
    public string PaymentIntentId { get; set; } = string.Empty;

    [Required]
    public string PaymentMethodToken { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = "Card";
}

public class CreatePaymentRequestDto
{
    public string PaymentMethod { get; set; } = "Card";
}

public class PaymentIntentResponseDto
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public decimal OrderTotalAmount { get; set; }
    public decimal AdvancePercentage { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool RequiresConfirmation { get; set; } = true;
}

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalReference { get; set; } = string.Empty;
    public decimal OrderTotalAmount { get; set; }
    public decimal AdvancePercentage { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OrderResponseDto Order { get; set; } = null!;
}
