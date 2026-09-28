using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class Shipment
{
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
}
