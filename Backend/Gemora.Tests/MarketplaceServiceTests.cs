
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

public class MarketplaceServiceTests
{
    [Fact]
    public async Task C2_01_OnlyApprovedAvailableGemsAreDisplayed()
    {
        // Arrange - isolated test database
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"MarketplaceTest_{Guid.NewGuid()}")
                .Options;

        await using var db = new ApplicationDbContext(options);

        var seller = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test Seller",
            Email = "seller@example.test",
            Role = UserRoles.Seller
        };

        var buyer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test Buyer",
            Email = "buyer@example.test",
            Role = UserRoles.Buyer
        };

        var availableGem = new GemListing
        {
            Id = 1001,
            SellerId = seller.Id,
            Seller = seller,
            Title = "Test Blue Sapphire",
            GemType = "Sapphire",
            Price = 150000m,
            Currency = "LKR",
            Status = GemListingStatuses.Approved
        };

        var unapprovedGem = new GemListing
        {
            Id = 1002,
            SellerId = seller.Id,
            Seller = seller,
            Title = "Unapproved Test Ruby",
            GemType = "Ruby",
            Price = 120000m,
            Status = GemListingStatuses.PendingVerification
        };

        var reservedGem = new GemListing
        {
            Id = 1003,
            SellerId = seller.Id,
            Seller = seller,
            Title = "Reserved Test Sapphire",
            GemType = "Sapphire",
            Price = 200000m,
            Status = GemListingStatuses.Approved
        };

        db.Users.AddRange(seller, buyer);

        db.GemListings.AddRange(
            availableGem,
            unapprovedGem,
            reservedGem
        );

        db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer.Id,
            SellerId = seller.Id,
            GemListingId = reservedGem.Id,
            Buyer = buyer,
            Seller = seller,
            GemListing = reservedGem,
            Status = OrderStatuses.Confirmed
        });

        await db.SaveChangesAsync();

        // Act - execute the actual marketplace service
        var service = new MarketplaceService(db);

        var result = await service.SearchAsync(
            new MarketplaceSearchQueryDto
            {
                Page = 1,
                PageSize = 12
            }
        );

        // Assert - only the approved and unreserved gem appears
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(1001, result.Items[0].Id);
        Assert.True(result.Items[0].IsAvailable);
    }
}
