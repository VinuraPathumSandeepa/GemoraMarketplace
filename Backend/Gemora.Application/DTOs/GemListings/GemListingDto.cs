namespace Gemora.Application.DTOs.GemListings;

public class GemListingDto
{
    public int Id { get; set; }

    public Guid SellerId { get; set; }

    public string SellerName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string GemType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal CaratWeight { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Clarity { get; set; } = string.Empty;

    public string Cut { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;


    // ============================================================
    // GEM IMAGE / CERTIFICATE EVIDENCE
    // ============================================================

    public string? PrimaryImageUrl { get; set; }

    public string? CertificateNumber { get; set; }

    public string? CertificateAuthority { get; set; }

    public string? CertificateUrl { get; set; }


    // ============================================================
    // WORKFLOW / AUDIT
    // ============================================================

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}