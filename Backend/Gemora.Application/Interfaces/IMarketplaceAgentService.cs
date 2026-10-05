using Gemora.Application.DTOs.Marketplace;

namespace Gemora.Application.Interfaces;

public interface IMarketplaceAgentService
{
    Task<MarketplaceAgentResponseDto> AssistAsync(MarketplaceAgentRequestDto request);
}
