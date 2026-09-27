namespace Gemora.Domain.Entities;

public class ShippingPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public Shipment Shipment { get; set; } = null!;

    public string RiskLevel { get; set; } = "low";

    public string RecommendedServiceType { get; set; } = string.Empty;

    public bool InsuranceRecommended { get; set; }

    public decimal RecommendedCoverage { get; set; }

    public string Requirements { get; set; } = string.Empty;

    public string Warnings { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
