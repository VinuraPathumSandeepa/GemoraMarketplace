namespace Gemora.Domain.Entities;

public class InsuranceRecord
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }

    // Coverage Details
    public decimal DeclaredValue { get; set; }
    public decimal CoverageAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CoverageType { get; set; } = "Standard"; // Standard, Premium, Comprehensive

    // Policy Information
    public string? PolicyNumber { get; set; }
    public string? PolicyReference { get; set; }
    public string? ProviderName { get; set; }
    public decimal PremiumAmount { get; set; }
    public DateTime? PolicyStartDate { get; set; }
    public DateTime? PolicyEndDate { get; set; }

    // Status
    public string Status { get; set; } = "Pending"; // Pending, Active, Claimed, Cancelled

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Shipment? Shipment { get; set; }
}
