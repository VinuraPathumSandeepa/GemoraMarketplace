<<<<<<< Updated upstream
using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;

=======
>>>>>>> Stashed changes
namespace Gemora.Domain.Entities;

public class ShippingPlan
{
<<<<<<< Updated upstream
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ShipmentId { get; set; }

    [Required]
    [MaxLength(20)]
    public RiskLevel RiskLevel { get; set; }

    [Required]
    [MaxLength(100)]
    public string RecommendedServiceType { get; set; } = string.Empty;

    [Required]
    public bool InsuranceRecommended { get; set; }

    [Required]
    public decimal RecommendedCoverage { get; set; }

    [Required]
    public string Requirements { get; set; } = "[]";

    [Required]
    public string Warnings { get; set; } = "[]";

    [Required]
    public DateTime GeneratedAt { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "PendingAdminApproval";

    public DateTime? ApprovedAt { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // Navigation property
    public virtual Shipment Shipment { get; set; } = null!;
=======
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
>>>>>>> Stashed changes
}
