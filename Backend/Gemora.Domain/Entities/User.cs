namespace Gemora.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Buyer
    // Seller
    // Gemologist
    // ExportOfficer
    // Admin
    public string Role { get; set; } = "Buyer";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ============================================================
    // SELLER -> GEM LISTINGS
    // ============================================================

    // A Seller can create multiple gem listings.
    public ICollection<GemListing> GemListings { get; set; }
        = new List<GemListing>();

    // ============================================================
    // GEMOLOGIST -> GEM VERIFICATIONS
    // ============================================================

    // A Gemologist can review multiple verification requests.
    public ICollection<GemVerification> GemVerifications { get; set; }
        = new List<GemVerification>();
}