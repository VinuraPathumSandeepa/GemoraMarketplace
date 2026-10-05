namespace Gemora.Application.DTOs.Marketplace;

public class MarketplaceGemResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string GemType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal CaratWeight { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Clarity { get; set; } = string.Empty;
    public string Cut { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
    public string? CertificateNumber { get; set; }
    public string? CertificateAuthority { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}
