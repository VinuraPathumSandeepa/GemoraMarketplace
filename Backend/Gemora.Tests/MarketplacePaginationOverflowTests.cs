
using System;
using System.Threading.Tasks;

using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gemora.Tests;

public class MarketplacePaginationOverflowTests
{
    private static async Task<ApplicationDbContext>
        CreateTestDbAsync()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    "C2_BUG_002_" + Guid.NewGuid().ToString("N"))
                .Options;

        var db = new ApplicationDbContext(options);

        var seller = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Marketplace Seller",
            Email = "c2-bug-002-seller@example.test",
            Role = UserRoles.Seller,
            IsEmailVerified = true
        };

        var gem = new GemListing
        {
            Id = 8001,
            SellerId = seller.Id,
            Seller = seller,

            Title = "Pagination QA Blue Sapphire",
            GemType = "Sapphire",
            Description = "Available gemstone for pagination QA",

            CaratWeight = 2.50m,
            Color = "Blue",
            Clarity = "VVS1",
            Cut = "Oval",

            Price = 250000m,
            Currency = "LKR",

            CountryCode = "LK",
            Region = "Western",

            Status = GemListingStatuses.Approved
        };

        db.Users.Add(seller);
        db.GemListings.Add(gem);

        await db.SaveChangesAsync();

        return db;
    }

    // ==============================================
    // CONTROL TEST: NORMAL FIRST PAGE
    // ==============================================

    [Fact]
    public async Task C2_BUG_002_FirstPage_ReturnsAvailableGem()
    {
        await using var db = await CreateTestDbAsync();

        var service = new MarketplaceService(db);

        var result = await service.SearchAsync(
            new MarketplaceSearchQueryDto
            {
                Page = 1,
                PageSize = 50
            }
        );

        Assert.Single(result.Items);
        Assert.Equal(8001, result.Items[0].Id);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
    }

    // ==============================================
    // NEGATIVE TEST: VERY LARGE PAGE NUMBERS
    // ==============================================

    [Theory]
    [InlineData(2147483647)]
    [InlineData(1000000000)]
    public async Task C2_BUG_002_LargePage_ReturnsEmptyWithoutOverflow(
        int requestedPage)
    {
        await using var db = await CreateTestDbAsync();

        var service = new MarketplaceService(db);

        var result = await service.SearchAsync(
            new MarketplaceSearchQueryDto
            {
                Page = requestedPage,
                PageSize = 50
            }
        );

        // A page this large cannot contain results.
        Assert.Empty(result.Items);

        // Metadata must remain correct.
        Assert.Equal(requestedPage, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(1, result.TotalPages);

        // Search must not modify existing listings.
        Assert.Equal(
            1,
            await db.GemListings.CountAsync()
        );
    }
}
