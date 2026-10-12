
using System;
using System.Threading.Tasks;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gemora.Tests;

public class MarketplaceGemDetailsTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static User CreateSeller()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test Gem Seller",
            Email = "gem-seller@example.test",
            Role = UserRoles.Seller
        };
    }

    private static GemListing CreateGem(
        User seller,
        int id,
        string status)
    {
        return new GemListing
        {
            Id = id,
            SellerId = seller.Id,
            Seller = seller,
            Title = "Test Blue Sapphire",
            GemType = "Sapphire",
            Description = "Test gemstone for C2-03",
            CaratWeight = 2.50m,
            Color = "Blue",
            Clarity = "VVS1",
            Cut = "Oval",
            Price = 150000m,
            Currency = "LKR",
            CountryCode = "LK",
            Region = "Western",
            Status = status
        };
    }

    // TEST 01: A nonexistent gem is not returned
    [Fact]
    public async Task C2_03_01_MissingGem_ReturnsNull()
    {
        await using var db = CreateDb();
        var service = new MarketplaceService(db);

        var result = await service.GetByIdAsync(999999999);

        Assert.Null(result);
    }

    // TEST 02: Draft/unapproved gem details are hidden
    [Fact]
    public async Task C2_03_02_UnapprovedGem_ReturnsNull()
    {
        await using var db = CreateDb();

        var seller = CreateSeller();
        var gem = CreateGem(
            seller, 2002, GemListingStatuses.Draft);

        db.Users.Add(seller);
        db.GemListings.Add(gem);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);
        var result = await service.GetByIdAsync(gem.Id);

        Assert.Null(result);
    }

    // TEST 03: Approved and available gem shows correct details
    [Fact]
    public async Task C2_03_03_ApprovedGem_ReturnsDetails()
    {
        await using var db = CreateDb();

        var seller = CreateSeller();
        var gem = CreateGem(
            seller, 2003, GemListingStatuses.Approved);

        db.Users.Add(seller);
        db.GemListings.Add(gem);
        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);
        var result = await service.GetByIdAsync(gem.Id);

        Assert.NotNull(result);
        Assert.Equal(gem.Id, result.Id);
        Assert.Equal(gem.Title, result.Title);
        Assert.Equal(gem.Price, result.Price);
        Assert.Equal("Test Gem Seller", result.SellerName);
        Assert.True(result.IsAvailable);
    }

    // TEST 04: A reserved gem remains viewable in order history
    [Fact]
    public async Task C2_03_04_ReservedGem_RemainsViewable()
    {
        await using var db = CreateDb();

        var seller = CreateSeller();
        var buyer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test Buyer",
            Email = "gem-buyer@example.test",
            Role = UserRoles.Buyer
        };

        var gem = CreateGem(
            seller, 2004, GemListingStatuses.Approved);

        db.Users.AddRange(seller, buyer);
        db.GemListings.Add(gem);

        db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer.Id,
            SellerId = seller.Id,
            GemListingId = gem.Id,
            Buyer = buyer,
            Seller = seller,
            GemListing = gem,
            Status = OrderStatuses.Confirmed,
            TotalAmount = gem.Price,
            Currency = gem.Currency
        });

        await db.SaveChangesAsync();

        var service = new MarketplaceService(db);
        var result = await service.GetByIdAsync(gem.Id);

        Assert.NotNull(result);
        Assert.Equal(gem.Id, result.Id);
        Assert.False(result.IsAvailable);
    }
}
