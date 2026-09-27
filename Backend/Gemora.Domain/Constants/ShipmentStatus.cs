namespace Gemora.Domain.Constants;

public static class ShipmentStatus
{
    public const string Pending = "Pending";
    public const string Planning = "Planning";
    public const string ReadyForBooking = "ReadyForBooking";
    public const string Booked = "Booked";
    public const string PickedUp = "PickedUp";
    public const string InTransit = "InTransit";
    public const string CustomsHold = "CustomsHold";
    public const string OutForDelivery = "OutForDelivery";
    public const string Delivered = "Delivered";
    public const string DeliveryFailed = "DeliveryFailed";
    public const string Cancelled = "Cancelled";
    public const string Exception = "Exception";
}
