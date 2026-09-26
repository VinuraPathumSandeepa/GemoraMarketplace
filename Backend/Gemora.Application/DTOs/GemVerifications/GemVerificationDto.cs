namespace Gemora.Application.DTOs.GemVerifications;

public class GemVerificationDto
{
    // Verification information
    public int VerificationId { get; set; }

    public string Decision { get; set; } = string.Empty;

    public string? ReviewNotes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }


    // ============================================================
    // GEM LISTING INFORMATION
    // ============================================================

    public int GemListingId { get; set; }

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

    public string ListingStatus { get; set; } = string.Empty;


    // ============================================================
    // AI VERIFICATION INFORMATION
    // ============================================================

    public string AiStatus { get; set; } = string.Empty;

    public string? AiSuggestedGemType { get; set; }

    public decimal? AiConfidenceScore { get; set; }

    public string? AiFindings { get; set; }

    public string? AiRiskFlags { get; set; }


    // ============================================================
    // GEMOLOGIST INFORMATION
    // ============================================================

    public Guid? GemologistId { get; set; }

    public string? GemologistName { get; set; }
}