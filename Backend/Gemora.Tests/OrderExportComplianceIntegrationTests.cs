using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gemora.Tests;

public class OrderExportComplianceIntegrationTests
{
    private class FakeFileStorageService : IFileStorageService
    {
        public Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
            => Task.FromResult($"test-key{extension}");

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream?>(new MemoryStream());

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class FakeComplianceRulesService : IComplianceRulesService
    {
        public Task<ComplianceEvaluationResult> EvaluateAsync(Guid userId, Guid exportRequestId)
        {
            return Task.FromResult(new ComplianceEvaluationResult
            {
                Success = true,
                Check = new ComplianceCheckResultDto
                {
                    IsComplete = true,
                    MissingRequirements = new List<string>(),
                    InvalidDocuments = new List<string>(),
                    Warnings = new List<string>()
                }
            });
        }
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static (User seller1, User seller2, User buyer1, User buyer2, User officer, GemListing listing, Order eligibleOrder)
        SeedScenario(ApplicationDbContext context)
    {
        var seller1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller1@gemora.com",
            FullName = "Primary Seller",
            Role = UserRoles.Seller
        };

        var seller2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller2@gemora.com",
            FullName = "Other Seller",
            Role = UserRoles.Seller
        };

        var buyer1 = new User
        {
            Id = Guid.NewGuid(),
            Email = "buyer1@gemora.com",
            FullName = "Authorized Buyer",
            Role = UserRoles.Buyer
        };

        var buyer2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "buyer2@gemora.com",
            FullName = "Unrelated Buyer",
            Role = UserRoles.Buyer
        };

        var officer = new User
        {
            Id = Guid.NewGuid(),
            Email = "officer@gemora.com",
            FullName = "Compliance Officer",
            Role = UserRoles.ExportOfficer
        };

        var listing = new GemListing
        {
            Id = 101,
            SellerId = seller1.Id,
            Title = "Ceylon Royal Blue Sapphire 3.5ct",
            Price = 4500.00m,
            Currency = "USD",
            Status = GemListingStatuses.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var eligibleOrder = new Order
        {
            Id = Guid.NewGuid(),
            SellerId = seller1.Id,
            BuyerId = buyer1.Id,
            GemListingId = listing.Id,
            TotalAmount = 4500.00m,
            Currency = "USD",
            Status = OrderStatuses.Confirmed,
            FulfillmentStatus = FulfillmentStatuses.Preparing,
            ShippingCountryCode = "US",
            ShippingAddress = "742 Evergreen Terrace",
            ShippingRegion = "Oregon",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(seller1, seller2, buyer1, buyer2, officer);
        context.GemListings.Add(listing);
        context.Orders.Add(eligibleOrder);
        context.SaveChanges();

        return (seller1, seller2, buyer1, buyer2, officer, listing, eligibleOrder);
    }

    [Fact]
    public async Task Test01_SellerCanCreateExportRequestFromOwnEligibleOrder()
    {
        using var context = CreateDbContext();
        var (seller1, _, _, _, _, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        var result = await service.CreateExportRequestFromOrderAsync(seller1.Id, eligibleOrder.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Request);
        Assert.Equal(eligibleOrder.Id, result.Request.OrderId);
        Assert.Equal("Sri Lanka", result.Request.OriginCountry);
        Assert.Equal("United States", result.Request.DestinationCountry);
        Assert.Equal(4500.00m, result.Request.DeclaredValue);
        Assert.Equal("USD", result.Request.Currency);
        Assert.Equal("Draft", result.Request.Status);

        var savedInDb = await context.ExportRequests.FirstOrDefaultAsync(e => e.OrderId == eligibleOrder.Id);
        Assert.NotNull(savedInDb);
        Assert.Equal(seller1.Id, savedInDb.RequestedByUserId);
    }

    [Fact]
    public async Task Test02_SellerCannotCreateExportRequestFromAnotherSellersOrder()
    {
        using var context = CreateDbContext();
        var (_, seller2, _, _, _, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        var result = await service.CreateExportRequestFromOrderAsync(seller2.Id, eligibleOrder.Id);

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task Test03_BuyerCannotCreateExportRequestFromOrder()
    {
        using var context = CreateDbContext();
        var (_, _, buyer1, _, _, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        var result = await service.CreateExportRequestFromOrderAsync(buyer1.Id, eligibleOrder.Id);

        Assert.False(result.Success);
        Assert.Equal("FORBIDDEN", result.ErrorCode);
    }

    [Fact]
    public async Task Test04_DuplicateExportRequestForSameOrderIsRejected()
    {
        using var context = CreateDbContext();
        var (seller1, _, _, _, _, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        var first = await service.CreateExportRequestFromOrderAsync(seller1.Id, eligibleOrder.Id);
        Assert.True(first.Success);

        var second = await service.CreateExportRequestFromOrderAsync(seller1.Id, eligibleOrder.Id);
        Assert.False(second.Success);
        Assert.Equal("EXPORT_REQUEST_ALREADY_EXISTS", second.ErrorCode);
    }

    [Fact]
    public async Task Test05_BuyerCanReadExportTrackingForOwnOrder()
    {
        using var context = CreateDbContext();
        var (seller1, _, buyer1, _, _, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        // Create export request first
        await service.CreateExportRequestFromOrderAsync(seller1.Id, eligibleOrder.Id);

        var tracking = await service.GetExportTrackingForOrderAsync(eligibleOrder.Id, buyer1.Id);

        Assert.NotNull(tracking);
        Assert.Equal(eligibleOrder.Id, tracking.OrderId);
        Assert.Equal("Draft", tracking.Status);
        Assert.Equal("United States", tracking.DestinationCountry);
        Assert.True(tracking.RequiresExport);
        Assert.NotEmpty(tracking.StageDescription);
    }

    [Fact]
    public async Task Test06_BuyerCannotReadTrackingForAnotherBuyersOrder()
    {
        using var context = CreateDbContext();
        var (seller1, _, _, buyer2, _, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        await service.CreateExportRequestFromOrderAsync(seller1.Id, eligibleOrder.Id);

        // buyer2 is not buyer1 (the order owner)
        var tracking = await service.GetExportTrackingForOrderAsync(eligibleOrder.Id, buyer2.Id);

        Assert.Null(tracking);
    }

    [Fact]
    public async Task Test07_BuyerResponseDoesNotExposeInternalAiOrOfficerData()
    {
        using var context = CreateDbContext();
        var (seller1, _, buyer1, _, _, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        await service.CreateExportRequestFromOrderAsync(seller1.Id, eligibleOrder.Id);
        var tracking = await service.GetExportTrackingForOrderAsync(eligibleOrder.Id, buyer1.Id);

        Assert.NotNull(tracking);
        // Verify buyer DTO type contains no internal prompt / tool / notes fields
        var properties = typeof(BuyerExportTrackingDto).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("AgentWorkflowId", properties);
        Assert.DoesNotContain("ReviewNotes", properties);
        Assert.DoesNotContain("OfficerNotes", properties);
        Assert.DoesNotContain("AiPrompt", properties);
        Assert.DoesNotContain("ToolCalls", properties);
    }

    [Fact]
    public async Task Test08_ExistingSellerManualExportLifecycleStillWorks()
    {
        using var context = CreateDbContext();
        var (seller1, _, _, _, _, _, _) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        var manualDto = new CreateExportRequestDto
        {
            OriginCountry = "Sri Lanka",
            DestinationCountry = "United Kingdom",
            DeclaredValue = 2500.00m,
            Currency = "USD",
            Purpose = "Gemological Exhibition"
        };

        var result = await service.CreateExportRequestAsync(seller1.Id, manualDto);

        Assert.True(result.Success);
        Assert.NotNull(result.Request);
        Assert.Null(result.Request.OrderId);
        Assert.Equal("United Kingdom", result.Request.DestinationCountry);

        var list = await service.GetMyExportRequestsAsync(seller1.Id);
        Assert.True(list.Success);
        Assert.NotNull(list.Requests);
        Assert.Single(list.Requests);
        Assert.Null(list.Requests[0].OrderId);
    }

    [Fact]
    public async Task Test09_ExportOfficerCanStillReviewLinkedRequest()
    {
        using var context = CreateDbContext();
        var (seller1, _, _, _, officer, _, eligibleOrder) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());
        var officerService = new ExportOfficerService(context, new FakeFileStorageService(), new FakeComplianceRulesService());

        var createResult = await service.CreateExportRequestFromOrderAsync(seller1.Id, eligibleOrder.Id);
        Assert.True(createResult.Success);
        var exportRequestId = createResult.Request!.Id;

        // Add document and submit to enter review queue
        await service.AddComplianceDocumentAsync(seller1.Id, exportRequestId, new CreateComplianceDocumentDto
        {
            DocumentType = "GemologyCertificate",
            DocumentNumber = "GC-98213",
            Issuer = "National Gem and Jewellery Authority",
            IssueDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        });

        await service.SubmitExportRequestAsync(seller1.Id, exportRequestId);

        // Officer reviews request
        var queueResult = await officerService.GetReviewQueueAsync(officer.Id);
        Assert.True(queueResult.Success);
        Assert.NotNull(queueResult.Requests);
        Assert.Contains(queueResult.Requests, r => r.Id == exportRequestId);

        var detailResult = await officerService.GetRequestForReviewAsync(officer.Id, exportRequestId);
        Assert.True(detailResult.Success);
        Assert.NotNull(detailResult.Request);
        Assert.Equal(eligibleOrder.Id, detailResult.Request.OrderId);
        Assert.Equal($"GEM-{eligibleOrder.Id.ToString("N")[..8].ToUpperInvariant()}", detailResult.Request.OrderNumber);
        Assert.Equal("Ceylon Royal Blue Sapphire 3.5ct", detailResult.Request.GemListingTitle);
    }

    [Fact]
    public async Task Test10_DomesticOrderIsNotEligibleForExportClearance()
    {
        using var context = CreateDbContext();
        var (seller1, _, buyer1, _, _, listing, _) = SeedScenario(context);
        var service = new ExportComplianceService(context, new FakeFileStorageService());

        var domesticOrder = new Order
        {
            Id = Guid.NewGuid(),
            SellerId = seller1.Id,
            BuyerId = buyer1.Id,
            GemListingId = listing.Id,
            TotalAmount = 1200.00m,
            Currency = "LKR",
            Status = OrderStatuses.Confirmed,
            FulfillmentStatus = FulfillmentStatuses.Preparing,
            ShippingCountryCode = "LK",
            ShippingAddress = "Colombo 03",
            CreatedAt = DateTime.UtcNow
        };
        context.Orders.Add(domesticOrder);
        await context.SaveChangesAsync();

        var result = await service.CreateExportRequestFromOrderAsync(seller1.Id, domesticOrder.Id);
        Assert.False(result.Success);
        Assert.Equal("ORDER_NOT_ELIGIBLE_FOR_EXPORT", result.ErrorCode);

        var tracking = await service.GetExportTrackingForOrderAsync(domesticOrder.Id, buyer1.Id);
        Assert.NotNull(tracking);
        Assert.Equal("NotRequired", tracking.Status);
        Assert.False(tracking.RequiresExport);
    }
}
