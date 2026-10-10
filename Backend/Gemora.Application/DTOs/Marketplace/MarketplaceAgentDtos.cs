namespace Gemora.Application.DTOs.Marketplace;


// ============================================================
// MARKETPLACE AGENT REQUEST
// ============================================================

public class MarketplaceAgentRequestDto
{
    public string Query { get; set; } =
        string.Empty;

    public string? GemType { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? Color { get; set; }

    public string? Cut { get; set; }
}


// ============================================================
// MARKETPLACE AGENT RECOMMENDATION
// ============================================================

public class MarketplaceAgentRecommendationDto
{
    public int GemListingId { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } =
        string.Empty;

    public string? PrimaryImageUrl { get; set; }

    public List<string> Reasons { get; set; } =
        new();

    public List<string> Tradeoffs { get; set; } =
        new();
}


// ============================================================
// MARKETPLACE AGENT RESPONSE
// ============================================================

public class MarketplaceAgentResponseDto
{
    public string Summary { get; set; } =
        string.Empty;

    public List<MarketplaceAgentRecommendationDto>
        Recommendations { get; set; } =
            new();

    public Dictionary<string, string>
        FiltersApplied { get; set; } =
            new();

    public List<string> Warnings { get; set; } =
        new();

    public bool UsedFallback { get; set; }
}