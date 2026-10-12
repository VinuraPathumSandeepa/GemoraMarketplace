
using System;
using System.IO;
using System.Threading.Tasks;
using Gemora.Application.DTOs.GemListings;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gemora.Tests;

public class GemListingCreationTests
{
    [Fact]
    public async Task C1_01_ValidSellerListing_CreatesDraft()
    {
        // Arrange
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

        await using var db = new ApplicationDbContext(options);

        var seller = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Seller",
            Email = "c101@example.test",
            Role = UserRoles.Seller
        };

        db.Users.Add(seller);
        await db.SaveChangesAsync();

        var request = new CreateGemListingDto
        {
            Title = "Natural Blue Sapphire",
            GemType = "Sapphire",
            Description = "Natural blue sapphire for testing.",
            CaratWeight = 2.50m,
            Color = "Blue",
            Clarity = "VVS1",
            Cut = "Oval",
            Price = 150000m,
            Currency = "LKR"
        };

        var service = new GemListingService(
            db,
            new FakeFileStorage()
        );

        // Act
        var result = await service.CreateAsync(
            seller.Id,
            request
        );

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal(seller.Id, result.SellerId);
        Assert.Equal("Natural Blue Sapphire", result.Title);
        Assert.Equal(GemListingStatuses.Draft, result.Status);

        var saved = await db.GemListings
            .SingleAsync(g => g.Id == result.Id);

        Assert.Equal(150000m, saved.Price);
        Assert.Equal("LKR", saved.Currency);
        Assert.Equal(seller.Id, saved.SellerId);
        Assert.Equal(GemListingStatuses.Draft, saved.Status);
    }

    private sealed class FakeFileStorage
        : Gemora.Domain.Interfaces.IFileStorageService
    {
        public Task<string> SaveGemImageAsync(
            Stream stream, string name,
            string type, long length)
            => throw new NotSupportedException();

        public Task<string> SaveCertificateAsync(
            Stream stream, string name,
            string type, long length)
            => throw new NotSupportedException();

        public Task DeleteFileAsync(string? url)
            => Task.CompletedTask;
    }
}
