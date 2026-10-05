namespace Gemora.Application.DTOs.Orders;

public class CreateShipmentRequestDto
{
    public string CourierName { get; set; } = string.Empty;

    public string TrackingNumber { get; set; } = string.Empty;

    public string? TrackingUrl { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    public string? DispatchNote { get; set; }
}