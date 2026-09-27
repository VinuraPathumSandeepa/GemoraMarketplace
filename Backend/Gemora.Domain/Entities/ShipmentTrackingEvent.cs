namespace Gemora.Domain.Entities;

public class ShipmentTrackingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ShipmentId { get; set; }

    public Shipment Shipment { get; set; } = null!;

    public string Status { get; set; } = string.Empty;

    public string LocationText { get; set; } = string.Empty;

    public string? ExternalEventCode { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
