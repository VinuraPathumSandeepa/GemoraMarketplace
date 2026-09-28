using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class InsuranceRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ShipmentId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string PolicyReference { get; set; } = string.Empty;

    [Required]
    public decimal DeclaredValue { get; set; }

    [Required]
    public decimal CoverageAmount { get; set; }

    [Required]
    [MaxLength(50)]
    public InsuranceCoverageType CoverageType { get; set; }

    [Required]
    [MaxLength(50)]
    public InsuranceStatus Status { get; set; }

    [Required]
    public decimal PremiumAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual Shipment Shipment { get; set; } = null!;
}
