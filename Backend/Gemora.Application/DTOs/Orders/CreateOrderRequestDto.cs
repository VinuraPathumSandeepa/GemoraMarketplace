using System.ComponentModel.DataAnnotations;
namespace Gemora.Application.DTOs.Orders;

public class CreateOrderRequestDto
{
    public int GemListingId { get; set; }

    public DeliveryDetailsRequestDto DeliveryDetails
        { get; set; } = new();


    // =============================================
    // LEGACY SUPPORT
    // Keep temporarily so old frontend/API calls
    // do not immediately break after the merge.
    // =============================================

    public string? ShippingAddress { get; set; }

    public string? ShippingRegion { get; set; }

    public string? ShippingCountryCode { get; set; }
}