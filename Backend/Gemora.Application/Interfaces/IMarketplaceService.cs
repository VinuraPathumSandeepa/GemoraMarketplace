using Gemora.Application.DTOs.Marketplace;

namespace Gemora.Application.Interfaces;

using Gemora.Application.DTOs.Marketplace;

public interface IMarketplaceService
{
    Task<PagedMarketplaceResponseDto> SearchAsync(MarketplaceSearchQueryDto query);
    Task<MarketplaceGemResponseDto?> GetByIdAsync(int id);

    Task<MarketplaceStatsDto> GetStatsAsync();
}
