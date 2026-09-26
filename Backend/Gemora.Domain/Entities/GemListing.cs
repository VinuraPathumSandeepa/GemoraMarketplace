namespace Gemora.Domain.Entities;

public class GemListing
{
    public int Id { get; set; }

    // Seller who owns this listing.
    // User.Id is Guid, so SellerId must also be Guid.
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

    // Draft
    // PendingVerification
    // Approved
    // ChangesRequested
    // Rejected
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // ============================================================
    // NAVIGATION PROPERTY - SELLER
    // ============================================================

    public User Seller { get; set; } = null!;

    // ============================================================
    // NAVIGATION PROPERTY - VERIFICATION HISTORY
    // ============================================================

    // One listing can have multiple verification records.
    //
    // Example:
    // First verification  -> ChangesRequested
    // Seller edits
    // Second verification -> Approved
    //
    // Keeping multiple records gives us verification history.
    public ICollection<GemVerification> Verifications { get; set; }
        = new List<GemVerification>();
}