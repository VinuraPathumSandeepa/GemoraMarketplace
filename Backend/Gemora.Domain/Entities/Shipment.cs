<<<<<<< Updated upstream
using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;
=======
using Gemora.Domain.Entities;
>>>>>>> Stashed changes

namespace Gemora.Domain.Entities;

public class Shipment
{
<<<<<<< Updated upstream
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ShipmentNumber { get; set; } = string.Empty;

    [Required]
    public Guid BuyerUserId { get; set; }

    [Required]
    public Guid SellerUserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public decimal DeclaredValue { get; set; }

    [Required]
    [MaxLength(20)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PackageDescription { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SelectedService { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CourierName { get; set; } = string.Empty;

    public string? ExternalShipmentReference { get; set; }

    public string? TrackingNumber { get; set; }

    [Required]
    [MaxLength(50)]
    public ShipmentStatus Status { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<ShipmentTrackingEvent> TrackingEvents { get; set; } = new List<ShipmentTrackingEvent>();
    public virtual ICollection<InsuranceRecord> InsuranceRecords { get; set; } = new List<InsuranceRecord>();
    public virtual ShippingPlan? ShippingPlan { get; set; }
=======
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    
    // Origin and Destination
    public string OriginAddress { get; set; } = string.Empty;
    public string OriginRegion { get; set; } = string.Empty;
    public string OriginCountryCode { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string DestinationRegion { get; set; } = string.Empty;
    public string DestinationCountryCode { get; set; } = string.Empty;
    
    // Package Details
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = "USD";
    public string PackageDescription { get; set; } = string.Empty;
    public decimal? PackageWeight { get; set; }
    public string? PackageDimensions { get; set; }
    public string SpecialHandlingNotes { get; set; } = string.Empty;
    
    // Shipping Service
    public string PreferredService { get; set; } = string.Empty;
    public bool ExportRequired { get; set; }
    
    // Status
    public string Status { get; set; } = "Pending"; // Pending, PlanGenerated, PlanApproved, InTransit, Delivered, Cancelled, Exception
    public string? RiskLevel { get; set; } // Low, Medium, High, Critical
    
    // Tracking
    public string? TrackingNumber { get; set; }
    public string? CourierName { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    
    // Navigation properties
    public User? Seller { get; set; }
    public User? Buyer { get; set; }
    public ShippingPlan? ShippingPlan { get; set; }
    public InsuranceRecord? InsuranceRecord { get; set; }
    public ICollection<ShipmentTrackingEvent>? TrackingEvents { get; set; }
>>>>>>> Stashed changes
}
