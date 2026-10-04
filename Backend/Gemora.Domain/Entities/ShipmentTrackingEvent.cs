using System.ComponentModel.DataAnnotations;

namespace Gemora.Domain.Entities;

public class ShipmentTrackingEvent
{
    [Key]
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    
    // Event Details
    public string EventType { get; set; } = string.Empty; // Created, PickedUp, InTransit, OutForDelivery, Delivered, Exception, Returned
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime OccurredAt { get; set; }

    [Required]
    public DateTime RecordedAt { get; set; }

    // Navigation property
    public virtual Shipment Shipment { get; set; } = null!;
}
