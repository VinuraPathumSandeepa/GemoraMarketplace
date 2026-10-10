using Gemora.Application.DTOs.GemVerifications;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Tests;

public class GemVerificationServiceTests
{
    // ============================================================
    // GEM-VER-01
    // NON-GEMOLOGIST CANNOT REVIEW
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_01_NonGemologistCannotReview()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        var buyer = CreateUser(
            UserRoles.Buyer,
            "buyer@gemora.test",
            "Test Buyer"
        );

        context.Users.AddRange(
            seller,
            buyer
        );

        var listing = CreatePendingListing(
            seller
        );

        context.GemListings.Add(listing);

        var verification = CreatePendingVerification(
            listing
        );

        context.GemVerifications.Add(
            verification
        );

        await context.SaveChangesAsync();

        var service =
            new GemVerificationService(
                context
            );

        var dto =
            new ReviewGemVerificationDto
            {
                Decision =
                    GemListingStatuses.Approved,

                ReviewNotes =
                    "Looks valid."
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<
                UnauthorizedAccessException>(
                () =>
                    service.ReviewVerificationAsync(
                        verification.Id,
                        buyer.Id,
                        dto
                    )
            );

        // Assert
        Assert.Equal(
            "Only Gemologists can review gem verification requests.",
            exception.Message
        );
    }

    // ============================================================
    // GEM-VER-02
    // UNKNOWN VERIFICATION RETURNS NULL
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_02_UnknownVerification_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();

        var gemologist = CreateUser(
            UserRoles.Gemologist,
            "gemologist@gemora.test",
            "Test Gemologist"
        );

        context.Users.Add(gemologist);

        await context.SaveChangesAsync();

        var service =
            new GemVerificationService(
                context
            );

        var dto =
            new ReviewGemVerificationDto
            {
                Decision =
                    GemListingStatuses.Approved
            };

        // Act
        var result =
            await service
                .ReviewVerificationAsync(
                    999999,
                    gemologist.Id,
                    dto
                );

        // Assert
        Assert.Null(result);
    }

    // ============================================================
    // GEM-VER-03
    // INVALID DECISION IS REJECTED
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_03_InvalidDecision_IsRejected()
    {
        // Arrange
        await using var context = CreateContext();

        var data =
            await SeedPendingVerificationAsync(
                context
            );

        var service =
            new GemVerificationService(
                context
            );

        var dto =
            new ReviewGemVerificationDto
            {
                Decision = "AutoVerified",
                ReviewNotes =
                    "Invalid test decision."
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.ReviewVerificationAsync(
                        data.Verification.Id,
                        data.Gemologist.Id,
                        dto
                    )
            );

        // Assert
        Assert.Equal(
            "Decision must be Approved, ChangesRequested, or Rejected.",
            exception.Message
        );
    }

    // ============================================================
    // GEM-VER-04
    // CHANGES REQUESTED MUST HAVE NOTES
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_04_ChangesRequestedWithoutNotes_IsRejected()
    {
        // Arrange
        await using var context = CreateContext();

        var data =
            await SeedPendingVerificationAsync(
                context
            );

        var service =
            new GemVerificationService(
                context
            );

        var dto =
            new ReviewGemVerificationDto
            {
                Decision =
                    GemListingStatuses
                        .ChangesRequested,

                ReviewNotes = "   "
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.ReviewVerificationAsync(
                        data.Verification.Id,
                        data.Gemologist.Id,
                        dto
                    )
            );

        // Assert
        Assert.Equal(
            "Review notes are required when requesting changes or rejecting a listing.",
            exception.Message
        );
    }

    // ============================================================
    // GEM-VER-05
    // REJECTION MUST HAVE NOTES
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_05_RejectedWithoutNotes_IsRejected()
    {
        // Arrange
        await using var context = CreateContext();

        var data =
            await SeedPendingVerificationAsync(
                context
            );

        var service =
            new GemVerificationService(
                context
            );

        var dto =
            new ReviewGemVerificationDto
            {
                Decision =
                    GemListingStatuses.Rejected,

                ReviewNotes = null
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.ReviewVerificationAsync(
                        data.Verification.Id,
                        data.Gemologist.Id,
                        dto
                    )
            );

        // Assert
        Assert.Equal(
            "Review notes are required when requesting changes or rejecting a listing.",
            exception.Message
        );
    }

    // ============================================================
    // GEM-VER-06
    // APPROVAL UPDATES VERIFICATION AND LISTING
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_06_Approved_UpdatesVerificationAndListing()
    {
        // Arrange
        await using var context = CreateContext();

        var data =
            await SeedPendingVerificationAsync(
                context
            );

        var service =
            new GemVerificationService(
                context
            );

        var dto =
            new ReviewGemVerificationDto
            {
                Decision = " approved ",
                ReviewNotes =
                    "Evidence reviewed and accepted."
            };

        // Act
        var result =
            await service
                .ReviewVerificationAsync(
                    data.Verification.Id,
                    data.Gemologist.Id,
                    dto
                );

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            GemListingStatuses.Approved,
            result.Decision
        );

        Assert.Equal(
            GemListingStatuses.Approved,
            result.ListingStatus
        );

        Assert.Equal(
            data.Gemologist.Id,
            result.GemologistId
        );

        Assert.NotNull(
            result.ReviewedAt
        );

        var listing =
            await context.GemListings
                .SingleAsync(
                    g =>
                        g.Id ==
                        data.Listing.Id
                );

        Assert.Equal(
            GemListingStatuses.Approved,
            listing.Status
        );
    }

    // ============================================================
    // GEM-VER-07
    // CHANGES REQUESTED SAVES GEMOLOGIST MESSAGE
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_07_ChangesRequested_SavesReviewNotes()
    {
        // Arrange
        await using var context = CreateContext();

        var data =
            await SeedPendingVerificationAsync(
                context
            );

        var service =
            new GemVerificationService(
                context
            );

        const string reviewMessage =
            "Please upload a clearer certificate image.";

        var dto =
            new ReviewGemVerificationDto
            {
                Decision =
                    GemListingStatuses
                        .ChangesRequested,

                ReviewNotes =
                    $"  {reviewMessage}  "
            };

        // Act
        var result =
            await service
                .ReviewVerificationAsync(
                    data.Verification.Id,
                    data.Gemologist.Id,
                    dto
                );

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            GemListingStatuses
                .ChangesRequested,
            result.Decision
        );

        Assert.Equal(
            reviewMessage,
            result.ReviewNotes
        );

        Assert.Equal(
            GemListingStatuses
                .ChangesRequested,
            result.ListingStatus
        );
    }

    // ============================================================
    // GEM-VER-08
    // DUPLICATE REVIEW IS BLOCKED
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_VER_08_DuplicateReview_IsRejected()
    {
        // Arrange
        await using var context = CreateContext();

        var data =
            await SeedPendingVerificationAsync(
                context
            );

        var service =
            new GemVerificationService(
                context
            );

        var firstReview =
            new ReviewGemVerificationDto
            {
                Decision =
                    GemListingStatuses.Approved,

                ReviewNotes =
                    "Approved after review."
            };

        await service
            .ReviewVerificationAsync(
                data.Verification.Id,
                data.Gemologist.Id,
                firstReview
            );

        var secondReview =
            new ReviewGemVerificationDto
            {
                Decision =
                    GemListingStatuses.Rejected,

                ReviewNotes =
                    "Attempting second decision."
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.ReviewVerificationAsync(
                        data.Verification.Id,
                        data.Gemologist.Id,
                        secondReview
                    )
            );

        // Assert
        Assert.Equal(
            "This verification request has already been reviewed.",
            exception.Message
        );
    }

    // ============================================================
    // TEST DATABASE
    // ============================================================

    private static ApplicationDbContext
        CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<
                ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"GemVerificationTests-{Guid.NewGuid()}"
                )
                .Options;

        return new ApplicationDbContext(
            options
        );
    }

    // ============================================================
    // SEED COMPLETE PENDING VERIFICATION
    // ============================================================

    private static async Task<
        PendingVerificationSeed>
        SeedPendingVerificationAsync(
            ApplicationDbContext context)
    {
        var seller = CreateUser(
            UserRoles.Seller,
            $"seller-{Guid.NewGuid()}@gemora.test",
            "Test Seller"
        );

        var gemologist = CreateUser(
            UserRoles.Gemologist,
            $"gemologist-{Guid.NewGuid()}@gemora.test",
            "Test Gemologist"
        );

        context.Users.AddRange(
            seller,
            gemologist
        );

        var listing =
            CreatePendingListing(
                seller
            );

        context.GemListings.Add(
            listing
        );

        var verification =
            CreatePendingVerification(
                listing
            );

        context.GemVerifications.Add(
            verification
        );

        await context.SaveChangesAsync();

        return new PendingVerificationSeed(
            seller,
            gemologist,
            listing,
            verification
        );
    }

    // ============================================================
    // USER FIXTURE
    // ============================================================

    private static User CreateUser(
        string role,
        string email,
        string fullName)
    {
        return new User
        {
            Id = Guid.NewGuid(),

            FullName = fullName,

            Email = email,

            PasswordHash =
                "test-password-hash",

            Role = role,

            PhoneNumber =
                "+94770000000",

            CountryCode = "LK",

            Region =
                "Western Province",

            IsEmailVerified = true,

            EmailVerifiedAt =
                DateTime.UtcNow,

            CreatedAt =
                DateTime.UtcNow
        };
    }

    // ============================================================
    // PENDING LISTING FIXTURE
    // ============================================================

    private static GemListing
        CreatePendingListing(
            User seller)
    {
        return new GemListing
        {
            SellerId = seller.Id,

            Seller = seller,

            Title = "Blue Topaz",

            GemType = "Topaz",

            Description =
                "Natural Blue Topaz used for verification tests.",

            CaratWeight = 5.28m,

            Color = "Vivid Blue",

            Clarity = "Eye Clean",

            Cut = "Oval Mixed Cut",

            Price = 125000m,

            Currency = "LKR",

            CountryCode = "LK",

            Region =
                "Ratnapura",

            PrimaryImageUrl =
                "/test/blue-topaz.jpg",

            CertificateNumber =
                "GMT-BT-2026-014",

            CertificateAuthority =
                "Gemora Gem Laboratory",

            CertificateUrl =
                "/test/blue-topaz.pdf",

            Status =
                GemListingStatuses
                    .PendingVerification,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow
        };
    }

    // ============================================================
    // VERIFICATION FIXTURE
    // ============================================================

    private static GemVerification
        CreatePendingVerification(
            GemListing listing)
    {
        return new GemVerification
        {
            GemListing = listing,

            Decision = "Pending",

            AiStatus = "NotStarted",

            CreatedAt =
                DateTime.UtcNow
        };
    }

    // ============================================================
    // SEED RESULT
    // ============================================================

    private sealed record
        PendingVerificationSeed(
            User Seller,
            User Gemologist,
            GemListing Listing,
            GemVerification Verification
        );
}