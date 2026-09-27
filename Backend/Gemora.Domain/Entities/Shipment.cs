using Gemora.Domain.Constants;

namespace Gemora.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public string ShipmentNumber { get; set; } = string.Empty;

    public Guid BuyerUserId { get; set; }

    public Guid SellerUserId { get; set; }

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public decimal DeclaredValue { get; set; }

    public string Currency { get; set; } = "USD";

    public string PackageDescription { get; set; } = string.Empty;

    public string SelectedService { get; set; } = string.Empty;

    public string CourierName { get; set; } = string.Empty;

    public string? ExternalShipmentReference { get; set; }

    public string? TrackingNumber { get; set; }

    public string Status { get; set; } = ShipmentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ShipmentTrackingEvent> TrackingEvents { get; set; } = new List<ShipmentTrackingEvent>();

    public ICollection<InsuranceRecord> InsuranceRecords { get; set; } = new List<InsuranceRecord>();

    public ShippingPlan? ShippingPlan { get; set; }
}
