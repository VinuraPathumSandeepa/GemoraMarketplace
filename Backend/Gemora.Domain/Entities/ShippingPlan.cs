namespace Gemora.Domain.Entities;

public class ShippingPlan
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }

    // AI Analysis Results
    public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string? RiskReasons { get; set; }
    public string RecommendedServiceType { get; set; } = "Standard";
    public bool InsuranceRecommended { get; set; }
    public decimal? RecommendedCoverageAmount { get; set; }
    public string? HandlingRequirements { get; set; }
    public string? RequiredDocuments { get; set; }
    public string? Warnings { get; set; }

    // Admin Approval
    public bool IsApproved { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? AdminNotes { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Shipment? Shipment { get; set; }
}
