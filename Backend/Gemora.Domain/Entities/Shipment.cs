namespace Gemora.Domain.Entities;

public class Shipment
{
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

    public string CourierName
        { get; set; } = string.Empty;


    public string TrackingNumber
        { get; set; } = string.Empty;


    public string? TrackingUrl
        { get; set; }


    public DateTime? ExpectedDeliveryDate
        { get; set; }

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
    public string? DispatchNote
        { get; set; }

    // Status
    public string Status { get; set; } = "Pending"; // Pending, PlanGenerated, PlanApproved, InTransit, Delivered, Cancelled, Exception
    public string? RiskLevel { get; set; } // Low, Medium, High, Critical

    // Tracking
    public string? TrackingNumber { get; set; }
    public string? CourierName { get; set; }
    public string? ExternalShipmentReference { get; set; }
    public string? SelectedService { get; set; }
    public string Status
        { get; set; } = string.Empty;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? BookedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt
        { get; set; }


    public DateTime? UpdatedAt
        { get; set; }


    public DateTime? HandedOverAt
        { get; set; }


    public DateTime? DeliveredAt
        { get; set; }


    public Order Order
        { get; set; } = null!;
    // Navigation properties
    public User? Seller { get; set; }
    public User? Buyer { get; set; }
    public ShippingPlan? ShippingPlan { get; set; }
    public InsuranceRecord? InsuranceRecord { get; set; }
    public ICollection<ShipmentTrackingEvent>? TrackingEvents { get; set; }
}
/*  */