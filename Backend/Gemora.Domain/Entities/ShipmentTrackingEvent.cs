namespace Gemora.Domain.Entities;

public class ShipmentTrackingEvent
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    
    // Event Details
    public string EventType { get; set; } = string.Empty; // Created, PickedUp, InTransit, OutForDelivery, Delivered, Exception, Returned
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Timestamps
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public Shipment? Shipment { get; set; }
}
