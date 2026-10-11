namespace Gemora.Application.DTOs.Marketplace;

public class MarketplaceStatsDto
{
    public int AuthorizedSellers { get; set; }

    public int RegisteredBuyers { get; set; }

    public int ActiveGemListings { get; set; }

    public int SuccessfulTransactions { get; set; }
}