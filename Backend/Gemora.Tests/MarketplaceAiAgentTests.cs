
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Gemora.Application.Configuration;
using Gemora.Application.DTOs.MarketplaceAgent;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;

using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Xunit;

namespace Gemora.Tests;

public class MarketplaceAiAgentTests
{
    [Fact]
    public async Task C2_AI_01_SearchTool_ReturnsOnlyAvailableApprovedGems()
    {
        // ARRANGE - isolated test database
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    "C2_AI_01_" + Guid.NewGuid().ToString("N"))
                .Options;

        await using var db = new ApplicationDbContext(options);

        var seller = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Seller",
            Email = "seller-ai@example.test",
            Role = UserRoles.Seller
        };

        var buyer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Buyer",
            Email = "buyer-ai@example.test",
            Role = UserRoles.Buyer
        };

        GemListing CreateGem(
            int id,
            string title,
            string status,
            decimal price)
        {
            return new GemListing
            {
                Id = id,
                SellerId = seller.Id,
                Seller = seller,
                Title = title,
                GemType = "Sapphire",
                Description = "QA test sapphire",
                CaratWeight = 2.5m,
                Color = "Blue",
                Clarity = "VVS1",
                Cut = "Oval",
                Price = price,
                Currency = "LKR",
                CountryCode = "LK",
                Region = "Western",
                Status = status
            };
        }

        var available = CreateGem(
            501, "Available Sapphire",
            GemListingStatuses.Approved, 280000m);

        var unapproved = CreateGem(
            502, "Unapproved Sapphire",
            GemListingStatuses.PendingVerification, 210000m);

        var reserved = CreateGem(
            503, "Reserved Sapphire",
            GemListingStatuses.Approved, 300000m);

        var expensive = CreateGem(
            504, "Expensive Sapphire",
            GemListingStatuses.Approved, 900000m);

        db.Users.AddRange(seller, buyer);

        db.GemListings.AddRange(
            available, unapproved, reserved, expensive);

        db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = buyer.Id,
            SellerId = seller.Id,
            GemListingId = reserved.Id,
            Buyer = buyer,
            Seller = seller,
            GemListing = reserved,
            Status = OrderStatuses.Pending,
            TotalAmount = reserved.Price,
            Currency = "LKR"
        });

        await db.SaveChangesAsync();

        // Controlled AI decision: use search_gems
        var fakeAi = new FakeMarketplaceAiClient();

        var service = new MarketplaceAgentService(
            db,
            fakeAi,
            Options.Create(new GeminiMarketplaceOptions
            {
                Model = "qa-test-model"
            }));

        // ACT
        var response = await service.AskAsync(
            buyer.Id,
            new MarketplaceAgentRequestDto
            {
                Message =
                    "Find available sapphires under LKR 500000"
            });

        // ASSERT - Correct backend tool selected
        Assert.Contains(
            MarketplaceAgentToolNames.SearchGems,
            response.ToolsUsed);

        Assert.NotNull(fakeAi.CapturedResult);
        Assert.True(fakeAi.CapturedResult!.Success);

        // ASSERT - Actual backend filtering result
        var data = JsonSerializer.SerializeToElement(
            fakeAi.CapturedResult.Data);

        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.Equal(1, data.GetArrayLength());

        var gem = data[0];

        Assert.Equal(
            501,
            gem.GetProperty("Id").GetInt32());

        Assert.Equal(
            "Available Sapphire",
            gem.GetProperty("Title").GetString());

        Assert.Equal(
            280000m,
            gem.GetProperty("Price").GetDecimal());

        Assert.True(
            gem.GetProperty("IsAvailable").GetBoolean());

        Assert.Equal(
            "Found matching available gemstones.",
            response.Message);
    }

    private sealed class FakeMarketplaceAiClient
        : IMarketplaceAiClient
    {
        public MarketplaceAgentToolResultDto? CapturedResult
        {
            get;
            private set;
        }

        public Task<MarketplaceAiDecisionDto> GetDecisionAsync(
            string userMessage,
            string systemPrompt,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MarketplaceAiDecisionDto
            {
                RequiresTool = true,
                ToolName = MarketplaceAgentToolNames.SearchGems,
                ToolArguments = JsonSerializer.SerializeToElement(
                    new
                    {
                        gemType = "Sapphire",
                        maxPrice = 500000m
                    })
            });
        }

        public Task<string> GenerateFinalResponseAsync(
            string userMessage,
            string systemPrompt,
            MarketplaceAgentToolResultDto toolResult,
            CancellationToken cancellationToken = default)
        {
            CapturedResult = toolResult;

            return Task.FromResult(
                "Found matching available gemstones.");
        }
    }
}
