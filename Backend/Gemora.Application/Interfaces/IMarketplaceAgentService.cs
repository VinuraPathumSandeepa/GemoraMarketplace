using LegacyRequest =
    Gemora.Application.DTOs.Marketplace.MarketplaceAgentRequestDto;

using LegacyResponse =
    Gemora.Application.DTOs.Marketplace.MarketplaceAgentResponseDto;

using ChatRequest =
    Gemora.Application.DTOs.MarketplaceAgent.MarketplaceAgentRequestDto;

using ChatResponse =
    Gemora.Application.DTOs.MarketplaceAgent.MarketplaceAgentResponseDto;

namespace Gemora.Application.Interfaces;

public interface IMarketplaceAgentService
{
    // =========================================================
    // EXISTING / LEGACY RECOMMENDATION ENDPOINT
    // =========================================================

    Task<LegacyResponse> RecommendAsync(
        Guid buyerId,
        LegacyRequest request,
        CancellationToken cancellationToken = default
    );


    // =========================================================
    // NEW GEMINI TOOL-BASED AGENT
    // =========================================================

    Task<ChatResponse> AskAsync(
        Guid buyerId,
        ChatRequest request,
        CancellationToken cancellationToken = default
    );
}