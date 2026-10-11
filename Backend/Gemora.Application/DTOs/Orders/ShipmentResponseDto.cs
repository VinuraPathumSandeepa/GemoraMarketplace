namespace Gemora.Application.DTOs.Orders;

public class ShipmentResponseDto
{
    public Guid Id { get; set; }

    public string CourierName { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public string? TrackingUrl { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public string? DispatchNote { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? HandedOverAt { get; set; }

    public DateTime? DeliveredAt { get; set; }
}