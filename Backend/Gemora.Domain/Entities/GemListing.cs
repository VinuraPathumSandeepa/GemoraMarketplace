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

    // Gemstone image uploaded by the seller.
    public string? PrimaryImageUrl { get; set; }

    // Optional certificate information.
    public string? CertificateNumber { get; set; }

    public string? CertificateAuthority { get; set; }

    public string? CertificateUrl { get; set; }

    /*
     * Listing location.
     *
     * Store the country as the ISO 3166-1 alpha-2 code.
     * Examples:
     * LK = Sri Lanka
     * US = United States
     * GB = United Kingdom
     * AU = Australia
     */
    public string CountryCode { get; set; } = string.Empty;

    /*
     * Province / State / Region where the gemstone
     * is currently located.
     *
     * Examples:
     * Sabaragamuwa Province
     * Western Province
     * Bangkok
     */
    public string Region { get; set; } = string.Empty;

    public string Status { get; set; } = GemListingStatuses.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Seller who owns this listing.
    public User Seller { get; set; } = null!;

    // Verification history for this listing.
    public ICollection<GemVerification> Verifications { get; set; }
        = new List<GemVerification>();
}