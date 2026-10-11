namespace Gemora.Domain.Enums;

public enum ShipmentStatus
{
    Pending,
    Planning,
    ReadyForBooking,
    Booked,
    PickedUp,
    InTransit,
    CustomsHold,
    OutForDelivery,
    Delivered,
    DeliveryFailed,
    Cancelled,
    Exception
}
