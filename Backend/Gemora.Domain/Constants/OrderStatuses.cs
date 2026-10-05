namespace Gemora.Domain.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";

    public const string Completed = "Completed";
    public const string Confirmed = "Confirmed";
    public const string AwaitingPayment = "AwaitingPayment";
    public const string Paid = "Paid";
    public const string PreparingForShipment = "PreparingForShipment";
    public const string ShipmentCreated = "ShipmentCreated";
    public const string InTransit = "InTransit";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Refunded = "Refunded";
    public const string Failed = "Failed";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Pending,
            Confirmed,
            AwaitingPayment,
            Paid,
            PreparingForShipment,
            ShipmentCreated,
            InTransit,
            Delivered,
            Cancelled,
            Refunded,
            Failed
        };

    public static readonly IReadOnlySet<string> NonBlocking =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Cancelled,
            Refunded,
            Failed
        };
}