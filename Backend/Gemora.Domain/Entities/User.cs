namespace Gemora.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Buyer";

    // Example: +94771234567
    public string PhoneNumber { get; set; } = string.Empty;

    // ISO 3166-1 alpha-2 country code.
    // Examples: LK, US, GB, AU
    public string CountryCode { get; set; } = string.Empty;

    // Province / State / Region.
    // Examples: Western Province, Sabaragamuwa Province
    public string Region { get; set; } = string.Empty;

    /*
     * Keep this true by default so that existing Gemora users,
     * seeded staff users and test accounts are not suddenly
     * blocked after the database migration.
     *
     * Later, new Buyer/Seller registrations will explicitly set
     * this to false until the email OTP is verified.
     */
    public bool IsEmailVerified { get; set; } = true;

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GemListing> GemListings { get; set; }
        = new List<GemListing>();

    public ICollection<GemVerification> GemVerifications { get; set; }
        = new List<GemVerification>();

    public ICollection<EmailVerificationCode> EmailVerificationCodes { get; set; }
        = new List<EmailVerificationCode>();
    
    public ICollection<Order> PurchasedOrders { get; set; }
        = new List<Order>();
    
    public ICollection<Order> SoldOrders { get; set; }
        = new List<Order>();
    
    public ICollection<Shipment> SellerShipments { get; set; }
        = new List<Shipment>();
    
    public ICollection<Shipment> BuyerShipments { get; set; }
        = new List<Shipment>();
}