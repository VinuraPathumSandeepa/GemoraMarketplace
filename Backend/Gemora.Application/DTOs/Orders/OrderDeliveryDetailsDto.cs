namespace Gemora.Application.DTOs.Orders;

public class OrderDeliveryDetailsDto
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
        = string.Empty;

    public string? NearestLandmark { get; set; }

    public string? DeliveryInstructions { get; set; }

    public bool SignatureRequired { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? LockedAt { get; set; }
}