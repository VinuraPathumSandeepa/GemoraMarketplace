namespace Gemora.Domain.Interfaces;

/// <summary>
/// Abstraction for LLM-based shipping risk assessment.
/// Isolates provider access behind an interface for testability and configuration flexibility.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Generates a structured shipping plan recommendation based on trusted backend data.
    /// Returns null if the provider is unavailable or encounters a non-transient error.
    /// Throws on validation failures that should trigger fallback.
    /// </summary>
    Task<LlmShippingPlanRecommendation?> GenerateShippingPlanAsync(
        LlmShippingPlanInput input,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Trusted backend data provided to the LLM. Never includes client-supplied ownership/value.
/// </summary>
public record LlmShippingPlanInput
{
    public Guid ShipmentId { get; init; }
    public decimal DeclaredValue { get; init; }
    public string Currency { get; init; } = "USD";
    public string OriginCountryCode { get; init; } = "";
    public string DestinationCountryCode { get; init; } = "";
    public bool ExportRequired { get; init; }
    public decimal? PackageWeight { get; init; }
    public string? SpecialHandlingNotes { get; init; }
    public string? GemType { get; init; }
    public string? ListingTitle { get; init; }
    public string SellerRole { get; init; } = "Seller";
    public string BuyerRole { get; init; } = "Buyer";
    public List<string> ApprovedServiceTypes { get; init; } = new();
}

/// <summary>
/// Structured output from LLM matching strict schema requirements.
/// Must be validated before persistence.
/// </summary>
public record LlmShippingPlanRecommendation
{
    public string RiskLevel { get; init; } = "Medium"; // Low, Medium, High, Critical
    public List<string> RiskReasons { get; init; } = new();
    public string RecommendedServiceType { get; init; } = "Standard";
    public bool InsuranceRecommended { get; init; }
    public decimal? RecommendedCoverageAmount { get; init; }
    public List<string> HandlingRequirements { get; init; } = new();
    public List<string> RequiredDocuments { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public string GenerationSource { get; init; } = "AI"; // AI vs FallbackRules
    public string? ExecutionSummary { get; init; } // Concise audit trail without chain-of-thought
}
