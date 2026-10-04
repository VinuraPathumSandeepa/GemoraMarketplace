namespace Gemora.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public int? GemListingId { get; set; }

    // Order Details
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, Delivered, Cancelled

    // Shipping Address
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingRegion { get; set; } = string.Empty;
    public string ShippingCountryCode { get; set; } = string.Empty;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public User? Buyer { get; set; }
    public User? Seller { get; set; }
    public GemListing? GemListing { get; set; }
    public ICollection<Shipment>? Shipments { get; set; }
}
