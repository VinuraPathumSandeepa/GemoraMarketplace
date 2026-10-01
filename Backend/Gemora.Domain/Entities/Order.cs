<<<<<<< Updated upstream
using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;
=======
using Gemora.Domain.Entities;
>>>>>>> Stashed changes

namespace Gemora.Domain.Entities;

public class Order
{
<<<<<<< Updated upstream
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid BuyerUserId { get; set; }

    [Required]
    public Guid SellerUserId { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public OrderStatus Status { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User? BuyerUser { get; set; }
    public virtual User? SellerUser { get; set; }
=======
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
>>>>>>> Stashed changes
}
