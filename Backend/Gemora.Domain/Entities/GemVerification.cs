namespace Gemora.Domain.Entities;

public class GemVerification
{
    public int Id { get; set; }

    // Gem listing being verified
    public int GemListingId { get; set; }

    // Human gemologist who reviewed the listing
    // Null until a gemologist completes the review
    public Guid? GemologistId { get; set; }

    // Pending / Approved / ChangesRequested / Rejected
    public string Decision { get; set; } = "Pending";

    // Human gemologist's comments
    public string? ReviewNotes { get; set; }

    // ------------------------------------------------------------
    // AI VERIFICATION DATA
    // These fields will be populated later by Vinura's AI Agent.
    // ------------------------------------------------------------

    public string? AiSuggestedGemType { get; set; }

    public decimal? AiConfidenceScore { get; set; }

    public string? AiFindings { get; set; }

    public string? AiRiskFlags { get; set; }

    // Tracks AI processing
    // NotStarted / Processing / Completed / Failed
    public string AiStatus { get; set; } = "NotStarted";

    // ------------------------------------------------------------
    // AUDIT INFORMATION
    // ------------------------------------------------------------

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? AiProcessedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    // Navigation properties
    public GemListing GemListing { get; set; } = null!;

    public User? Gemologist { get; set; }
}