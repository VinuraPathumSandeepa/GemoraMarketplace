using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Tests;

public class MarketplaceSearchTests
{
    [Fact]
    public async Task PageBeyondIntOffsetReturnsNoListings()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var seller = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Seller",
            Email = "seller@example.com",
            Role = UserRoles.Seller
        };
        context.Users.Add(seller);
        context.GemListings.Add(new GemListing
        {
            SellerId = seller.Id,
            Title = "Blue Sapphire",
            GemType = "Sapphire",
            Status = GemListingStatuses.Approved,
            Price = 1000
        });
        await context.SaveChangesAsync();

        var result = await new MarketplaceService(context).SearchAsync(
            new MarketplaceSearchQueryDto { Page = int.MaxValue, PageSize = 50 });

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(int.MaxValue, result.Page);
    }
}
