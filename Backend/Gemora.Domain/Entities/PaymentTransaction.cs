namespace Gemora.Domain.Entities;

public class PaymentTransaction
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Provider { get; set; }
        = string.Empty;

    public string ExternalReference { get; set; }
        = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; }
        = "LKR";

    public string Status { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Order Order { get; set; }
        = null!;
}