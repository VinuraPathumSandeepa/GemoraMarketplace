<<<<<<< Updated upstream
using System.ComponentModel.DataAnnotations;

=======
>>>>>>> Stashed changes
namespace Gemora.Domain.Entities;

public class ShipmentTrackingEvent
{
<<<<<<< Updated upstream
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid ShipmentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string LocationText { get; set; } = string.Empty;

    public string? ExternalEventCode { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime OccurredAt { get; set; }

    [Required]
    public DateTime RecordedAt { get; set; }

    // Navigation property
    public virtual Shipment Shipment { get; set; } = null!;
=======
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
>>>>>>> Stashed changes
}
