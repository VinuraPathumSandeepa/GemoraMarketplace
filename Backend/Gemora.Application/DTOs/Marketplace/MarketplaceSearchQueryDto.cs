using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.Marketplace;

public class MarketplaceSearchQueryDto
{
    [StringLength(100)] public string? Search { get; set; }
    [StringLength(100)] public string? GemType { get; set; }
    [StringLength(100)] public string? Color { get; set; }
    [StringLength(100)] public string? Cut { get; set; }
    [StringLength(2)] public string? CountryCode { get; set; }
    [Range(0, 1_000_000_000)] public decimal? MinPrice { get; set; }
    [Range(0, 1_000_000_000)] public decimal? MaxPrice { get; set; }
    [Range(0, 10000)] public decimal? MinCarat { get; set; }
    [Range(0, 10000)] public decimal? MaxCarat { get; set; }
    public string Sort { get; set; } = "newest";
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 50)] public int PageSize { get; set; } = 12;
}
