namespace Gemora.Application.DTOs.Orders;

public class OrderResponseDto
{
    public Guid Id { get; set; }

    // No OrderNumber DB column.
    // Generate a display-only number from UUID.
    public string OrderNumber =>
        $"GEM-{Id.ToString("N")[..8].ToUpperInvariant()}";

    public int? GemListingId { get; set; }

    public string GemTitle { get; set; }
        = string.Empty;

    public string? GemImageUrl { get; set; }

    public Guid BuyerId { get; set; }

    public string BuyerName { get; set; }
        = string.Empty;

    public Guid SellerId { get; set; }

    public string SellerName { get; set; }
        = string.Empty;

    // API name.
    // Database column is TotalAmount.
    public decimal AgreedPrice { get; set; }

    public string Currency { get; set; }
        = string.Empty;

    public string Status { get; set; }
        = string.Empty;

    public string ShippingAddress { get; set; }
        = string.Empty;

    public string ShippingRegion { get; set; }
        = string.Empty;

    public string ShippingCountryCode { get; set; }
        = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public List<OrderStatusHistoryDto> StatusHistory
    { get; set; } = [];


    public string FulfillmentStatus { get; set; }
        = string.Empty;

    public DateTime? HandedOverAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public OrderDeliveryDetailsDto? DeliveryDetails
    { get; set; }


    public ShipmentResponseDto? Shipment { get; set; }

    public List<FulfillmentStatusHistoryDto>
        FulfillmentHistory
    { get; set; }
            = new();



}