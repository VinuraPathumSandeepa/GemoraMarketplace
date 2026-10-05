using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.Orders;

public class CreateOrderRequestDto
{
    [Range(1, int.MaxValue)]
    public int GemListingId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string ShippingAddress { get; set; }
        = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string ShippingRegion { get; set; }
        = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string ShippingCountryCode { get; set; }
        = string.Empty;
}