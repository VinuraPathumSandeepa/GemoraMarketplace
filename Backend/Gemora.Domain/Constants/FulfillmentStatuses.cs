namespace Gemora.Domain.Constants;

public static class FulfillmentStatuses
{
    public const string Pending = "Pending";

    public const string Preparing = "Preparing";

    public const string ReadyForDispatch = "ReadyForDispatch";

    public const string HandedOverToCourier = "HandedOverToCourier";

    public const string InTransit = "InTransit";

    public const string OutForDelivery = "OutForDelivery";

    public const string Delivered = "Delivered";

    public const string DeliveryFailed = "DeliveryFailed";

    public const string Returned = "Returned";
}