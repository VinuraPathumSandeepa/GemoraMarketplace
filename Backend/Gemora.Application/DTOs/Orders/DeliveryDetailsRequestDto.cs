namespace Gemora.Application.DTOs.Orders;

public class DeliveryDetailsRequestDto
{
    public string RecipientName { get; set; }
        = string.Empty;

    public string RecipientPhone { get; set; }
        = string.Empty;

    public string? AlternatePhone { get; set; }

    public string AddressLine1 { get; set; }
        = string.Empty;

    public string? AddressLine2 { get; set; }

    public string City { get; set; }
        = string.Empty;

    public string District { get; set; }
        = string.Empty;

    public string Region { get; set; }
        = string.Empty;

    public string PostalCode { get; set; }
        = string.Empty;

    public string CountryCode { get; set; }
        = "LK";

    public string? NearestLandmark { get; set; }

    public string? DeliveryInstructions { get; set; }
}