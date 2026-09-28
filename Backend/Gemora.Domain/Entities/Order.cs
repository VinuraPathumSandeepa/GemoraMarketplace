using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;

namespace Gemora.Domain.Entities;

public class Order
{
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
}
