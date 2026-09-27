namespace Gemora.Domain.Entities;

public class InsuranceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public Shipment Shipment { get; set; } = null!;

    public string Provider { get; set; } = string.Empty;

    public string PolicyReference { get; set; } = string.Empty;

    public decimal DeclaredValue { get; set; }

    public decimal CoverageAmount { get; set; }

    public string CoverageType { get; set; } = "Standard";

    public string Status { get; set; } = "Active";

    public decimal PremiumAmount { get; set; }

    public string Currency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
