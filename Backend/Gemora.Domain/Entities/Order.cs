using Gemora.Domain.Constants;

namespace Gemora.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

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

    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public User Buyer { get; set; } = null!;

    public User Seller { get; set; } = null!;

    public GemListing? GemListing { get; set; }

    public ICollection<OrderStatusHistory> StatusHistory { get; set; }
        = new List<OrderStatusHistory>();
}