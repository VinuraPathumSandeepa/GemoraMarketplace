using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class ShippingPlan
{
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
}
