namespace Gemora.Domain.Entities;

public class OrderDeliveryDetails
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    // =============================
    // RECIPIENT
    // =============================

    public string RecipientName { get; set; }
        = string.Empty;

    public string RecipientPhone { get; set; }
        = string.Empty;

    public string? AlternatePhone { get; set; }


    // =============================
    // ADDRESS
    // =============================

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


    // =============================
    // OPTIONAL DELIVERY INFO
    // =============================

    public string? NearestLandmark { get; set; }

    public string? DeliveryInstructions { get; set; }


    // Gemstones are high-value goods
    public bool SignatureRequired { get; set; }
        = true;


    // =============================
    // AUDIT / LOCKING
    // =============================

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? LastUpdatedByUserId { get; set; }

    public DateTime? LockedAt { get; set; }


    // =============================
    // NAVIGATION
    // =============================

    public Order Order { get; set; }
        = null!;
}