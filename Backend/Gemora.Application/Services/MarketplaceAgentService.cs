using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Interfaces;

namespace Gemora.Application.Services;

// Controlled buyer-assistance agent. It can only reason over the eligible listings
// returned by IMarketplaceService; it never receives unrestricted database access.
public class MarketplaceAgentService : IMarketplaceAgentService
{
    private readonly IMarketplaceService _marketplace;
    public MarketplaceAgentService(IMarketplaceService marketplace) => _marketplace = marketplace;

    public async Task<MarketplaceAgentResponseDto> AssistAsync(MarketplaceAgentRequestDto request)
    {
        var queryText = request.Query.Trim();
        var all = await _marketplace.SearchAsync(new MarketplaceSearchQueryDto
        {
            Search = queryText,
            Page = 1,
            PageSize = 20,
            Sort = "newest"
        });

        // Safe fallback: when natural-language terms do not directly match listing text,
        // inspect a small backend-approved eligible set and rank it deterministically.
        if (all.Items.Count == 0)
        {
            all = await _marketplace.SearchAsync(new MarketplaceSearchQueryDto
            {
                Page = 1,
                PageSize = 20,
                Sort = "newest"
            });
        }

        var terms = queryText.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ranked = all.Items
            .Select(g => new
            {
                Gem = g,
                Score = terms.Count(t => (g.Title + " " + g.GemType + " " + g.Color + " " + g.Cut + " " + g.Description)
                    .Contains(t, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Gem.Price)
            .Take(request.MaxRecommendations)
            .ToList();

        var result = new MarketplaceAgentResponseDto
        {
            Summary = ranked.Count == 0
                ? "No eligible marketplace listings currently match your request."
                : $"I found {ranked.Count} eligible gemstone option(s) from the live marketplace.",
            FiltersApplied = new Dictionary<string, string> { ["buyerQuery"] = queryText }
        };

        foreach (var x in ranked)
        {
            result.Recommendations.Add(new MarketplaceRecommendationDto
            {
                GemListingId = x.Gem.Id,
                Title = x.Gem.Title,
                Reasons = new List<string>
                {
                    $"Verified and currently available {x.Gem.GemType}.",
                    $"{x.Gem.CaratWeight:0.##} ct, {x.Gem.Color}, {x.Gem.Cut} cut.",
                    $"Listed at {x.Gem.Currency} {x.Gem.Price:N2}."
                },
                Tradeoffs = new List<string>
                {
                    "AI assistance is advisory; review the gem details and certificate before ordering."
                }
            });
        }

        result.Warnings.Add("Recommendations are limited to backend-approved eligible listings and do not guarantee investment value.");
        return result;
    }
}
