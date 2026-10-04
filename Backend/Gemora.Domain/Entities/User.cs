namespace Gemora.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Buyer";

    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;


    // =========================================================
    // PROFILE PHOTO
    // =========================================================

    public string? ProfileImageUrl { get; set; }


    // =========================================================
    // EMAIL VERIFICATION
    // =========================================================

    public bool IsEmailVerified { get; set; } = true;

    public DateTime? EmailVerifiedAt { get; set; }


    // =========================================================
    // AUDIT
    // =========================================================

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;


    // =========================================================
    // RELATIONSHIPS
    // =========================================================

    public ICollection<GemListing> GemListings { get; set; } =
        new List<GemListing>();

    public ICollection<GemVerification> GemVerifications { get; set; } =
        new List<GemVerification>();

    public ICollection<EmailVerificationCode> EmailVerificationCodes { get; set; } =
        new List<EmailVerificationCode>();
}