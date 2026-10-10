using Gemora.Domain.Constants;

namespace Gemora.Domain.Entities;

public class GemListing
{
    public int Id { get; set; }

    public Guid SellerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string GemType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal CaratWeight { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Clarity { get; set; } = string.Empty;

    public string Cut { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = "LKR";


    // =========================================================
    // IMAGE
    // =========================================================

    public string? PrimaryImageUrl { get; set; }


    // =========================================================
    // CERTIFICATE
    // =========================================================

    public string? CertificateNumber { get; set; }

    public string? CertificateAuthority { get; set; }

    public string? CertificateUrl { get; set; }


    // =========================================================
    // LOCATION
    // =========================================================

    public string CountryCode { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;


    // =========================================================
    // LISTING STATUS
    // =========================================================

    public string Status { get; set; } =
        GemListingStatuses.Draft;


    // =========================================================
    // AUDIT
    // =========================================================

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }


    // =========================================================
    // RELATIONSHIPS
    // =========================================================

    public User Seller { get; set; } = null!;

    public ICollection<GemVerification> Verifications { get; set; }
        = new List<GemVerification>();

    // Component 2
    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}