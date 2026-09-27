namespace Gemora.Domain.Entities;

public class GemListing
{
    public int Id { get; set; }

    // ============================================================
    // SELLER
    // ============================================================

    public Guid SellerId { get; set; }


    // ============================================================
    // BASIC GEM INFORMATION
    // ============================================================

    public string Title { get; set; } = string.Empty;

    public string GemType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal CaratWeight { get; set; }

    public string Color { get; set; } = string.Empty;

    public string Clarity { get; set; } = string.Empty;

    public string Cut { get; set; } = string.Empty;


    // ============================================================
    // PRICE
    // ============================================================

    public decimal Price { get; set; }

    public string Currency { get; set; } = "LKR";


    // ============================================================
    // GEM IMAGE
    // ============================================================

    // URL/path of the primary gemstone image.
    // Actual upload/storage support will be implemented separately.
    public string? PrimaryImageUrl { get; set; }


    // ============================================================
    // CERTIFICATE / SUPPORTING EVIDENCE
    // ============================================================

    // Example:
    // GIA-123456789
    // NGJA-SL-2026-001
    public string? CertificateNumber { get; set; }

    // Name of the laboratory/authority shown on the certificate.
    public string? CertificateAuthority { get; set; }

    // URL/path of the uploaded certificate document.
    public string? CertificateUrl { get; set; }


    // ============================================================
    // WORKFLOW STATUS
    // ============================================================

    // Draft
    // PendingVerification
    // Approved
    // ChangesRequested
    // Rejected
    public string Status { get; set; } = "Draft";


    // ============================================================
    // AUDIT INFORMATION
    // ============================================================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }


    // ============================================================
    // NAVIGATION PROPERTIES
    // ============================================================

    public User Seller { get; set; } = null!;

    public ICollection<GemVerification> Verifications { get; set; }
        = new List<GemVerification>();
}