using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Gemora.Tests;

public class GemListingSubmissionTests
{
    private static async Task<(ApplicationDbContext Db, GemListingService Service, GemListing Listing, Guid SellerId)> CreateListingAsync(
        string? imageUrl)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var sellerId = Guid.NewGuid();
        db.Users.AddRange(
            new User { Id = sellerId, FullName = "Seller", Email = "seller@example.test", Role = UserRoles.Seller },
            new User { Id = Guid.NewGuid(), FullName = "Gemologist", Email = "gemologist@example.test", Role = UserRoles.Gemologist });
        var listing = new GemListing
        {
            SellerId = sellerId,
            Title = "Blue Sapphire",
            GemType = "Sapphire",
            Status = GemListingStatuses.Draft,
            PrimaryImageUrl = imageUrl,
            Price = 1000,
            Currency = "LKR"
        };
        db.GemListings.Add(listing);
        await db.SaveChangesAsync();
        return (db, new GemListingService(db, Mock.Of<IFileStorageService>()), listing, sellerId);
    }

    [Fact]
    public async Task MissingImageCannotEnterVerificationQueue()
    {
        var (db, service, listing, sellerId) = await CreateListingAsync(null);
        await using (db)
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SubmitForVerificationAsync(listing.Id, sellerId));

            Assert.Contains("gemstone image", error.Message);
            Assert.Equal(GemListingStatuses.Draft, listing.Status);
            Assert.Empty(await db.GemVerifications.ToListAsync());
        }
    }

    [Fact]
    public async Task SubmissionUpdatesStatusAndCreatesVisibleQueueRecordOnlyOnce()
    {
        var (db, service, listing, sellerId) = await CreateListingAsync("/uploads/gem-images/sapphire.jpg");
        await using (db)
        {
            var submitted = await service.SubmitForVerificationAsync(listing.Id, sellerId);

            Assert.Equal(GemListingStatuses.PendingVerification, submitted?.Status);
            Assert.Equal(GemListingStatuses.PendingVerification, listing.Status);
            Assert.Single(await new GemVerificationService(db).GetPendingVerificationsAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SubmitForVerificationAsync(listing.Id, sellerId));
            Assert.Single(await db.GemVerifications.ToListAsync());
        }
    }

    [Fact]
    public async Task SellerCannotSubmitAnotherSellersListing()
    {
        var (db, service, listing, _) = await CreateListingAsync("/uploads/gem-images/sapphire.jpg");
        await using (db)
        {
            Assert.Null(await service.SubmitForVerificationAsync(listing.Id, Guid.NewGuid()));
            Assert.Equal(GemListingStatuses.Draft, listing.Status);
            Assert.Empty(await db.GemVerifications.ToListAsync());
        }
    }
}
