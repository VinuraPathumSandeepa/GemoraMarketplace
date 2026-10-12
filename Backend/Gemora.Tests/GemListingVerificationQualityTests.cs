
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Gemora.Application.DTOs.GemListings;
using Gemora.Application.DTOs.GemVerifications;
using Gemora.Application.Services;
using Gemora.Domain.AI;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Gemora.Tests;

public class GemListingVerificationQualityTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CreateGemListingDto ValidRequest()
    {
        return new CreateGemListingDto
        {
            Title = "Natural Blue Sapphire",
            GemType = "Sapphire",
            Description = "Natural sapphire for QA testing.",
            CaratWeight = 2.5m,
            Color = "Blue",
            Clarity = "VVS1",
            Cut = "Oval",
            Price = 150000m,
            Currency = "LKR"
        };
    }

    // ===============================================
    // C1-02: INVALID INPUT VALIDATION
    // ===============================================

    [Theory]
    [InlineData("title")]
    [InlineData("price")]
    [InlineData("carat")]
    public void C1_02_InvalidGemAttributes_AreRejected(
        string field)
    {
        var dto = ValidRequest();

        switch (field)
        {
            case "title":
                dto.Title = "";
                break;

            case "price":
                dto.Price = 0;
                break;

            case "carat":
                dto.CaratWeight = 0;
                break;
        }

        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            errors,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.NotEmpty(errors);
    }

    // ===============================================
    // SHARED TEST DATA FOR VERIFICATION WORKFLOW
    // ===============================================

    private static (
        GemListing Gem,
        GemVerification Verification,
        User Seller,
        User Gemologist
    ) SeedPending(
        ApplicationDbContext db,
        bool hasImage = false)
    {
        var seller = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Seller",
            Email = $"seller-{Guid.NewGuid():N}@example.test",
            Role = UserRoles.Seller
        };

        var gemologist = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Gemologist",
            Email = $"gemologist-{Guid.NewGuid():N}@example.test",
            Role = UserRoles.Gemologist
        };

        var gem = new GemListing
        {
            SellerId = seller.Id,
            Seller = seller,
            Title = "QA Blue Sapphire",
            GemType = "Sapphire",
            Description = "Verification workflow test gemstone",
            CaratWeight = 2.5m,
            Color = "Blue",
            Clarity = "VVS1",
            Cut = "Oval",
            Price = 150000m,
            Currency = "LKR",
            Status = GemListingStatuses.PendingVerification,

            PrimaryImageUrl = hasImage
                ? "/qa-image.png"
                : null
        };

        var verification = new GemVerification
        {
            GemListing = gem,
            Decision = "Pending",
            AiStatus = "NotStarted"
        };

        db.Users.AddRange(seller, gemologist);
        db.GemListings.Add(gem);
        db.GemVerifications.Add(verification);
        db.SaveChanges();

        return (gem, verification, seller, gemologist);
    }

    // ===============================================
    // C1-03: AUTHORIZATION
    // ===============================================

    [Fact]
    public async Task C1_03_SellerCannotReviewVerification()
    {
        await using var db = CreateDb();
        var data = SeedPending(db);

        var service = new GemVerificationService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () =>
            {
                await service.ReviewVerificationAsync(
                    data.Verification.Id,
                    data.Seller.Id,
                    new ReviewGemVerificationDto
                    {
                        Decision = "Approved"
                    });
            });

        Assert.Equal(
            "Pending",
            data.Verification.Decision);

        Assert.Equal(
            GemListingStatuses.PendingVerification,
            data.Gem.Status);
    }

    // ===============================================
    // C1-04: HUMAN VERIFICATION APPROVAL
    // ===============================================

    [Fact]
    public async Task C1_04_GemologistCanApproveListing()
    {
        await using var db = CreateDb();
        var data = SeedPending(db);

        var service = new GemVerificationService(db);

        var result =
            await service.ReviewVerificationAsync(
                data.Verification.Id,
                data.Gemologist.Id,
                new ReviewGemVerificationDto
                {
                    Decision = "Approved",
                    ReviewNotes = "Verified by QA Gemologist."
                });

        Assert.NotNull(result);
        Assert.Equal("Approved", result.Decision);

        Assert.Equal(
            GemListingStatuses.Approved,
            result.ListingStatus);

        Assert.Equal(
            data.Gemologist.Id,
            result.GemologistId);

        Assert.NotNull(result.ReviewedAt);

        var savedGem = await db.GemListings
            .FindAsync(data.Gem.Id);

        Assert.NotNull(savedGem);
        Assert.Equal(
            GemListingStatuses.Approved,
            savedGem.Status);
    }

    // ===============================================
    // C1-05 A: MISSING REQUIRED EVIDENCE
    // ===============================================

    [Fact]
    public async Task C1_05_MissingImage_SkipsAiModel()
    {
        await using var db = CreateDb();
        var data = SeedPending(db, hasImage: false);

        var model = new Mock<IGemAiModelClient>();
        var reader = new Mock<IGemImageReader>();

        var agent = new GemVerificationAgent(
            db,
            new GemEvidenceValidator(),
            model.Object,
            reader.Object,
            NullLogger<GemVerificationAgent>.Instance);

        var result =
            await agent.AnalyzeAsync(data.Verification.Id);

        Assert.Equal(
            "NeedsMoreEvidence",
            result.Status);

        Assert.False(result.ImageAnalyzed);
        Assert.NotEmpty(result.ValidationIssues);

        Assert.Equal(
            "NeedsMoreEvidence",
            data.Verification.AiStatus);

        Assert.Equal(
            "Pending",
            data.Verification.Decision);

        model.Verify(
            x => x.AnalyzeAsync(
                It.IsAny<GemListing>(),
                It.IsAny<Stream?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ===============================================
    // C1-05 B: AI PROVIDER FAILURE
    // ===============================================

    [Fact]
    public async Task C1_05_AiFailure_PersistsFailedState()
    {
        await using var db = CreateDb();
        var data = SeedPending(db, hasImage: true);

        var model = new Mock<IGemAiModelClient>();
        var reader = new Mock<IGemImageReader>();

        reader.Setup(
            x => x.OpenImageAsync(
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GemImageReadResult?)null);

        model.Setup(
            x => x.AnalyzeAsync(
                It.IsAny<GemListing>(),
                It.IsAny<Stream?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Simulated AI provider failure"));

        var agent = new GemVerificationAgent(
            db,
            new GemEvidenceValidator(),
            model.Object,
            reader.Object,
            NullLogger<GemVerificationAgent>.Instance);

        var result = await agent.AnalyzeAsync(
            data.Verification.Id);

        Assert.Equal(
            "Failed",
            result.Status);

        Assert.Equal(
            "Failed",
            data.Verification.AiStatus);

        Assert.Equal(
            "Pending",
            data.Verification.Decision);

        Assert.Equal(
            GemListingStatuses.PendingVerification,
            data.Gem.Status);
    }
}
