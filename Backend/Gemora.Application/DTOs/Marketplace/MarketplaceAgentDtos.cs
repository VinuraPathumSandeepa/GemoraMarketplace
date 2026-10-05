using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.Marketplace;

public class MarketplaceAgentRequestDto
{
    [Required, StringLength(500, MinimumLength = 2)]
    public string Query { get; set; } = string.Empty;

    [Range(1, 5)]
    public int MaxRecommendations { get; set; } = 3;
}

public class MarketplaceRecommendationDto
{
    public int GemListingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = [];
    public List<string> Tradeoffs { get; set; } = [];
}

public class MarketplaceAgentResponseDto
{
    public string Summary { get; set; } = string.Empty;
    public List<MarketplaceRecommendationDto> Recommendations { get; set; } = [];
    public Dictionary<string, string> FiltersApplied { get; set; } = new();
    public List<string> Warnings { get; set; } = [];
}
