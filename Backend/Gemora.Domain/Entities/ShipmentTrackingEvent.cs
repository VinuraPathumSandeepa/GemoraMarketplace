using System.ComponentModel.DataAnnotations;

namespace Gemora.Domain.Entities;

public class ShipmentTrackingEvent
{
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
}
