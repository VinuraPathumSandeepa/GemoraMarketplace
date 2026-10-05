namespace Gemora.Application.DTOs.Marketplace;

public class PagedMarketplaceResponseDto
{
    public List<MarketplaceGemResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
