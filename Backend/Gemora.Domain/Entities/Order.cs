using Gemora.Domain.Constants;

namespace Gemora.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Only pre-upgrade conflicting records are exempt from the new unique indexes.
    // Application availability checks deliberately include these records.
    public bool IsLegacyDuplicate { get; set; }

    public Guid BuyerId { get; set; }

    public Guid SellerId { get; set; }

    public int? GemListingId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = "LKR";

    public string Status { get; set; } = OrderStatuses.Pending;

    public string ShippingAddress { get; set; } = string.Empty;

    public string ShippingRegion { get; set; } = string.Empty;

    public string ShippingCountryCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? PaymentDueAt { get; set; }
    public DateTime? BuyerMessageAt { get; set; }
    public DateTime? BuyerReadAt { get; set; }
    public DateTime? SellerReadAt { get; set; }

    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public User Buyer { get; set; } = null!;

    public User Seller { get; set; } = null!;

    public GemListing? GemListing { get; set; }

    public ICollection<OrderStatusHistory> StatusHistory { get; set; }
        = new List<OrderStatusHistory>();


    public string FulfillmentStatus { get; set; }
    = FulfillmentStatuses.Pending;

    public DateTime? HandedOverAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public OrderDeliveryDetails? DeliveryDetails { get; set; }




    public Shipment? Shipment { get; set; }

    public ICollection<FulfillmentStatusHistory> FulfillmentStatusHistory
    { get; set; } = new List<FulfillmentStatusHistory>();


}