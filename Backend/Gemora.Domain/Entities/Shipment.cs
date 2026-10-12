namespace Gemora.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }


    public string CourierName
        { get; set; } = string.Empty;


    public string TrackingNumber
        { get; set; } = string.Empty;


    public string? TrackingUrl
        { get; set; }


    public DateTime? ExpectedDeliveryDate
        { get; set; }


    public string? DispatchNote
        { get; set; }


    public string Status
        { get; set; } = string.Empty;


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
}