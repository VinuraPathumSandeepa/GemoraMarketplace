using Gemora.Application.DTOs.GemListings;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Tests;

public class GemListingServiceTests
{
    // ============================================================
    // GEM-LIST-01
    // SELLER CAN CREATE A DRAFT LISTING
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_01_SellerCreatesListing_AsDraft()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        context.Users.Add(seller);

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        var dto = CreateValidCreateDto();

        dto.Title = "  Blue Topaz  ";
        dto.Currency = " lkr ";

        // Attempt to inject file URLs through normal DTO.
        // The service should ignore these protected fields.
        dto.PrimaryImageUrl =
            "https://malicious.test/fake-image.jpg";

        dto.CertificateUrl =
            "https://malicious.test/fake-certificate.pdf";

        // Act
        var result =
            await service.CreateAsync(
                seller.Id,
                dto
            );

        // Assert
        Assert.Equal(
            seller.Id,
            result.SellerId
        );

        Assert.Equal(
            "Blue Topaz",
            result.Title
        );

        Assert.Equal(
            "LKR",
            result.Currency
        );

        Assert.Equal(
            GemListingStatuses.Draft,
            result.Status
        );

        Assert.Null(
            result.PrimaryImageUrl
        );

        Assert.Null(
            result.CertificateUrl
        );
    }

    // ============================================================
    // GEM-LIST-02
    // NON-SELLER CANNOT CREATE LISTING
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_02_BuyerCannotCreateListing()
    {
        // Arrange
        await using var context = CreateContext();

        var buyer = CreateUser(
            UserRoles.Buyer,
            "buyer@gemora.test",
            "Test Buyer"
        );

        context.Users.Add(buyer);

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        var dto = CreateValidCreateDto();

        // Act
        var exception =
            await Assert.ThrowsAsync<
                UnauthorizedAccessException>(
                () =>
                    service.CreateAsync(
                        buyer.Id,
                        dto
                    )
            );

        // Assert
        Assert.Equal(
            "Only sellers can create gem listings.",
            exception.Message
        );

        Assert.Empty(
            context.GemListings
        );
    }

    // ============================================================
    // GEM-LIST-03
    // SELLER LIST QUERY IS OWNERSHIP SCOPED
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_03_GetMyListings_ReturnsOnlyOwnListings()
    {
        // Arrange
        await using var context = CreateContext();

        var sellerOne = CreateUser(
            UserRoles.Seller,
            "seller1@gemora.test",
            "Seller One"
        );

        var sellerTwo = CreateUser(
            UserRoles.Seller,
            "seller2@gemora.test",
            "Seller Two"
        );

        context.Users.AddRange(
            sellerOne,
            sellerTwo
        );

        context.GemListings.AddRange(
            CreateListing(
                sellerOne,
                GemListingStatuses.Draft,
                "Blue Topaz"
            ),
            CreateListing(
                sellerOne,
                GemListingStatuses.Approved,
                "Blue Sapphire"
            ),
            CreateListing(
                sellerTwo,
                GemListingStatuses.Draft,
                "Yellow Sapphire"
            )
        );

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        // Act
        var results =
            await service.GetMyListingsAsync(
                sellerOne.Id
            );

        // Assert
        Assert.Equal(
            2,
            results.Count
        );

        Assert.All(
            results,
            listing =>
                Assert.Equal(
                    sellerOne.Id,
                    listing.SellerId
                )
        );

        Assert.DoesNotContain(
            results,
            listing =>
                listing.Title ==
                "Yellow Sapphire"
        );
    }

    // ============================================================
    // GEM-LIST-04
    // SELLER CANNOT READ ANOTHER SELLER'S LISTING
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_04_OtherSellerListing_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();

        var owner = CreateUser(
            UserRoles.Seller,
            "owner@gemora.test",
            "Listing Owner"
        );

        var otherSeller = CreateUser(
            UserRoles.Seller,
            "other@gemora.test",
            "Other Seller"
        );

        context.Users.AddRange(
            owner,
            otherSeller
        );

        var listing = CreateListing(
            owner,
            GemListingStatuses.Draft,
            "Blue Topaz"
        );

        context.GemListings.Add(
            listing
        );

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        // Act
        var result =
            await service.GetByIdAsync(
                listing.Id,
                otherSeller.Id
            );

        // Assert
        Assert.Null(result);
    }

    // ============================================================
    // GEM-LIST-05
    // DRAFT LISTING CAN BE UPDATED
    // PROTECTED FILE URLS MUST NOT BE OVERWRITTEN
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_05_UpdateDraft_PreservesProtectedFileUrls()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        context.Users.Add(seller);

        var listing = CreateListing(
            seller,
            GemListingStatuses.Draft,
            "Old Blue Topaz"
        );

        listing.PrimaryImageUrl =
            "/storage/original-image.jpg";

        listing.CertificateUrl =
            "/storage/original-certificate.pdf";

        context.GemListings.Add(
            listing
        );

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        var dto = new UpdateGemListingDto
        {
            Title =
                "Updated Blue Topaz",

            GemType =
                "Topaz",

            Description =
                "Updated gemstone listing used for C1 workflow testing.",

            CaratWeight =
                6.15m,

            Color =
                "Swiss Blue",

            Clarity =
                "Eye Clean",

            Cut =
                "Oval Mixed Cut",

            Price =
                150000m,

            Currency =
                "usd",

            CertificateNumber =
                "UPDATED-CERT-001",

            CertificateAuthority =
                "Updated Gem Laboratory",

            PrimaryImageUrl =
                "/malicious/replacement.jpg",

            CertificateUrl =
                "/malicious/replacement.pdf"
        };

        // Act
        var updated =
            await service.UpdateAsync(
                listing.Id,
                seller.Id,
                dto
            );

        // Assert
        Assert.True(updated);

        var stored =
            await context.GemListings
                .SingleAsync(
                    g =>
                        g.Id ==
                        listing.Id
                );

        Assert.Equal(
            "Updated Blue Topaz",
            stored.Title
        );

        Assert.Equal(
            "USD",
            stored.Currency
        );

        Assert.Equal(
            "/storage/original-image.jpg",
            stored.PrimaryImageUrl
        );

        Assert.Equal(
            "/storage/original-certificate.pdf",
            stored.CertificateUrl
        );
    }

    // ============================================================
    // GEM-LIST-06
    // PENDING LISTING CANNOT BE EDITED
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_06_PendingVerificationListing_CannotBeEdited()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        context.Users.Add(seller);

        var listing = CreateListing(
            seller,
            GemListingStatuses.PendingVerification,
            "Blue Topaz"
        );

        context.GemListings.Add(
            listing
        );

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        var dto = CreateValidUpdateDto();

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.UpdateAsync(
                        listing.Id,
                        seller.Id,
                        dto
                    )
            );

        // Assert
        Assert.Equal(
            "Only Draft or ChangesRequested listings can be edited.",
            exception.Message
        );
    }

    // ============================================================
    // GEM-LIST-07
    // DRAFT -> PENDING VERIFICATION
    // CREATES NEW VERIFICATION RECORD
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_07_SubmitDraft_CreatesPendingVerification()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        context.Users.Add(seller);

        var listing = CreateListing(
            seller,
            GemListingStatuses.Draft,
            "Blue Topaz"
        );

        context.GemListings.Add(
            listing
        );

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        // Act
        var result =
            await service
                .SubmitForVerificationAsync(
                    listing.Id,
                    seller.Id
                );

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            GemListingStatuses.PendingVerification,
            result.Status
        );

        var verification =
            await context.GemVerifications
                .SingleAsync(
                    v =>
                        v.GemListingId ==
                        listing.Id
                );

        Assert.Equal(
            "Pending",
            verification.Decision
        );

        Assert.Equal(
            "NotStarted",
            verification.AiStatus
        );

        Assert.Null(
            verification.GemologistId
        );

        Assert.Null(
            verification.ReviewedAt
        );
    }

    // ============================================================
    // GEM-LIST-08
    // CHANGES REQUESTED CAN BE RESUBMITTED
    // NEW VERIFICATION ATTEMPT MUST BE CREATED
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_08_ChangesRequested_CanBeResubmitted()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        context.Users.Add(seller);

        var listing = CreateListing(
            seller,
            GemListingStatuses.ChangesRequested,
            "Blue Topaz"
        );

        context.GemListings.Add(
            listing
        );

        var previousVerification =
            new GemVerification
            {
                GemListing = listing,

                Decision =
                    GemListingStatuses
                        .ChangesRequested,

                ReviewNotes =
                    "Please upload clearer evidence.",

                AiStatus =
                    "Completed",

                CreatedAt =
                    DateTime.UtcNow
                        .AddHours(-1),

                ReviewedAt =
                    DateTime.UtcNow
                        .AddMinutes(-30)
            };

        context.GemVerifications.Add(
            previousVerification
        );

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        // Act
        var result =
            await service
                .SubmitForVerificationAsync(
                    listing.Id,
                    seller.Id
                );

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            GemListingStatuses.PendingVerification,
            result.Status
        );

        var attempts =
            await context.GemVerifications
                .Where(
                    v =>
                        v.GemListingId ==
                        listing.Id
                )
                .OrderBy(
                    v =>
                        v.CreatedAt
                )
                .ToListAsync();

        Assert.Equal(
            2,
            attempts.Count
        );

        Assert.Equal(
            GemListingStatuses.ChangesRequested,
            attempts[0].Decision
        );

        Assert.Equal(
            "Pending",
            attempts[1].Decision
        );

        Assert.Equal(
            "NotStarted",
            attempts[1].AiStatus
        );
    }

    // ============================================================
    // GEM-LIST-09
    // APPROVED LISTING CANNOT BE RESUBMITTED
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_09_ApprovedListing_CannotBeSubmittedAgain()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        context.Users.Add(seller);

        var listing = CreateListing(
            seller,
            GemListingStatuses.Approved,
            "Blue Topaz"
        );

        context.GemListings.Add(
            listing
        );

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        // Act
        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    service.SubmitForVerificationAsync(
                        listing.Id,
                        seller.Id
                    )
            );

        // Assert
        Assert.Equal(
            "Only Draft or ChangesRequested listings can be submitted for verification.",
            exception.Message
        );
    }

    // ============================================================
    // GEM-LIST-10
    // UNKNOWN / NOT OWNED LISTING RETURNS NULL
    // ============================================================

    [Fact]
    [Trait("Component", "C1")]
    public async Task GEM_LIST_10_UnknownListing_SubmitReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();

        var seller = CreateUser(
            UserRoles.Seller,
            "seller@gemora.test",
            "Test Seller"
        );

        context.Users.Add(seller);

        await context.SaveChangesAsync();

        var service = CreateService(
            context
        );

        // Act
        var result =
            await service
                .SubmitForVerificationAsync(
                    999999,
                    seller.Id
                );

        // Assert
        Assert.Null(result);

        Assert.Empty(
            context.GemVerifications
        );
    }

    // ============================================================
    // SERVICE FACTORY
    //
    // File storage is intentionally null because these tests cover
    // create/read/update/submit workflow methods only.
    // No upload/delete storage operation is invoked.
    // ============================================================

    private static GemListingService
        CreateService(
            ApplicationDbContext context)
    {
        return new GemListingService(
            context,
            null!
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
                    $"GemListingTests-{Guid.NewGuid()}"
                )
                .Options;

        return new ApplicationDbContext(
            options
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

            CountryCode =
                "LK",

            Region =
                "Western Province",

            IsEmailVerified =
                true,

            EmailVerifiedAt =
                DateTime.UtcNow,

            CreatedAt =
                DateTime.UtcNow
        };
    }

    // ============================================================
    // LISTING FIXTURE
    // ============================================================

    private static GemListing CreateListing(
        User seller,
        string status,
        string title)
    {
        return new GemListing
        {
            SellerId =
                seller.Id,

            Seller =
                seller,

            Title =
                title,

            GemType =
                "Topaz",

            Description =
                "Natural Blue Topaz listing used for C1 workflow tests.",

            CaratWeight =
                5.28m,

            Color =
                "Vivid Blue",

            Clarity =
                "Eye Clean",

            Cut =
                "Oval Mixed Cut",

            Price =
                125000m,

            Currency =
                "LKR",

            CountryCode =
                "LK",

            Region =
                "Ratnapura",

            PrimaryImageUrl =
                "/storage/blue-topaz.jpg",

            CertificateNumber =
                "GMT-BT-2026-014",

            CertificateAuthority =
                "Gemora Gem Laboratory",

            CertificateUrl =
                "/storage/blue-topaz.pdf",

            Status =
                status,

            CreatedAt =
                DateTime.UtcNow,

            UpdatedAt =
                DateTime.UtcNow
        };
    }

    // ============================================================
    // CREATE DTO FIXTURE
    // ============================================================

    private static CreateGemListingDto
        CreateValidCreateDto()
    {
        return new CreateGemListingDto
        {
            Title =
                "Blue Topaz",

            GemType =
                "Topaz",

            Description =
                "Natural Blue Topaz listing used for software testing.",

            CaratWeight =
                5.28m,

            Color =
                "Vivid Blue",

            Clarity =
                "Eye Clean",

            Cut =
                "Oval Mixed Cut",

            Price =
                125000m,

            Currency =
                "LKR",

            CertificateNumber =
                "GMT-BT-2026-014",

            CertificateAuthority =
                "Gemora Gem Laboratory"
        };
    }

    // ============================================================
    // UPDATE DTO FIXTURE
    // ============================================================

    private static UpdateGemListingDto
        CreateValidUpdateDto()
    {
        return new UpdateGemListingDto
        {
            Title =
                "Updated Blue Topaz",

            GemType =
                "Topaz",

            Description =
                "Updated Blue Topaz listing used for software testing.",

            CaratWeight =
                5.50m,

            Color =
                "Swiss Blue",

            Clarity =
                "Eye Clean",

            Cut =
                "Oval Mixed Cut",

            Price =
                135000m,

            Currency =
                "LKR",

            CertificateNumber =
                "GMT-BT-2026-UPDATED",

            CertificateAuthority =
                "Gemora Gem Laboratory"
        };
    }
}