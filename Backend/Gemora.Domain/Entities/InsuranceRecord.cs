<<<<<<< Updated upstream
using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;

=======
>>>>>>> Stashed changes
namespace Gemora.Domain.Entities;

public class InsuranceRecord
{
<<<<<<< Updated upstream
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
=======
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    
    // Coverage Details
    public decimal CoverageAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CoverageType { get; set; } = "Standard"; // Standard, Premium, Comprehensive
    
    // Policy Information
    public string? PolicyNumber { get; set; }
    public string? ProviderName { get; set; }
    public DateTime? PolicyStartDate { get; set; }
    public DateTime? PolicyEndDate { get; set; }
    
    // Status
    public string Status { get; set; } = "Pending"; // Pending, Active, Claimed, Cancelled
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public Shipment? Shipment { get; set; }
>>>>>>> Stashed changes
}
