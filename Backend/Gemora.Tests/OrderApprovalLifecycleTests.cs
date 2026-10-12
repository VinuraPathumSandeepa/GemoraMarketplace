using Gemora.Application.DTOs.Orders;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gemora.Tests;

public class OrderApprovalLifecycleTests
{
    [Fact]
    public async Task LegacyConflictingPaidOrderBlocksNewRequestsAndPayment()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var order = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        await service.ConfirmAsync(order.Id, seed.Seller.Id, UserRoles.Seller, null);
        db.Orders.Add(new Order { BuyerId = seed.Other.Id, SellerId = seed.Seller.Id,
            GemListingId = seed.Gem.Id, Status = OrderStatuses.Paid, PaidAt = DateTime.UtcNow,
            IsLegacyDuplicate = true });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PayAsync(order.Id, seed.Buyer.Id, new()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id)));
        Assert.Empty(await db.PaymentTransactions.ToListAsync());
    }

    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

    private static async Task<(User Seller, User Buyer, User Other, GemListing Gem)> Seed(ApplicationDbContext db)
    {
        var seller = new User { FullName = "Seller", Email = "seller@test.example", Role = UserRoles.Seller };
        var buyer = new User { FullName = "Buyer", Email = "buyer@test.example", Role = UserRoles.Buyer };
        var other = new User { FullName = "Other buyer", Email = "other@test.example", Role = UserRoles.Buyer };
        var gem = new GemListing { Title = "Blue sapphire", SellerId = seller.Id, Seller = seller,
            Status = GemListingStatuses.Approved, Price = 100000, Currency = "LKR" };
        db.AddRange(seller, buyer, other, gem);
        await db.SaveChangesAsync();
        return (seller, buyer, other, gem);
    }

    private static CreateOrderRequestDto Request(int gemId) => new()
    {
        GemListingId = gemId,
        DeliveryDetails = new DeliveryDetailsRequestDto
        {
            RecipientName = "Buyer", RecipientPhone = "0771234567", AddressLine1 = "1 Main Street",
            City = "Colombo", District = "Colombo", Region = "Western", PostalCode = "00100", CountryCode = "LK"
        }
    };

    [Fact]
    public async Task MultipleBuyersCanRequestSameGem_ButDuplicateBuyerCannot()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var first = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        var second = await service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id));
        Assert.Equal(OrderStatuses.Pending, first.Status);
        Assert.Equal(OrderStatuses.Pending, second.Status);
        Assert.Equal(GemListingStatuses.Approved, seed.Gem.Status);
        var marketplace = await new MarketplaceService(db).SearchAsync(new() { Page = 1, PageSize = 12 });
        Assert.Contains(marketplace.Items, gem => gem.Id == seed.Gem.Id && gem.IsAvailable);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id)));
    }

    [Fact]
    public async Task ApprovalReservesExactlyOneOrder_RejectsCompetitors_AndNotifiesBoth()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var first = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        var second = await service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id));
        var before = DateTime.UtcNow;
        var approved = await service.ConfirmAsync(first.Id, seed.Seller.Id, UserRoles.Seller, null);
        var rejected = await service.GetForBuyerAsync(second.Id, seed.Other.Id);
        Assert.Equal(OrderStatuses.Confirmed, approved.Status);
        Assert.InRange(approved.PaymentDueAt!.Value, before.AddHours(3), DateTime.UtcNow.AddHours(3));
        Assert.NotNull(approved.BuyerMessageAt);
        Assert.Null(approved.BuyerReadAt);
        Assert.Equal(OrderStatuses.Rejected, rejected!.Status);
        Assert.Contains("another buyer", rejected.StatusHistory.Last().Reason);
        Assert.NotNull(rejected.BuyerMessageAt);
        var marketplace = await new MarketplaceService(db).SearchAsync(new() { Page = 1, PageSize = 12 });
        Assert.DoesNotContain(marketplace.Items, gem => gem.Id == seed.Gem.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync(second.Id, seed.Seller.Id, UserRoles.Seller, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id)));
    }

    [Fact]
    public async Task SellerCanOnlyDecideOwnOrders()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var order = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ConfirmAsync(order.Id, Guid.NewGuid(), UserRoles.Seller, null));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RejectAsync(order.Id, Guid.NewGuid(), "No", false));
        Assert.Equal(OrderStatuses.Pending, (await service.GetForBuyerAsync(order.Id, seed.Buyer.Id))!.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RejectionRequiresReason(string? reason)
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var order = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RejectAsync(order.Id, seed.Seller.Id, reason, false));
    }

    [Fact]
    public async Task RejectionPreservesReason_LeavesOtherRequestsAndListingAvailable()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var first = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        var second = await service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id));
        var result = await service.RejectAsync(first.Id, seed.Seller.Id, "  Cannot deliver to this location.  ", false);
        Assert.Equal("Cannot deliver to this location.", result.StatusHistory.Last().Reason);
        Assert.Equal(OrderStatuses.Pending, (await service.GetForBuyerAsync(second.Id, seed.Other.Id))!.Status);
        Assert.Equal(GemListingStatuses.Approved, seed.Gem.Status);
        Assert.Equal(OrderStatuses.Pending, (await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id))).Status);
    }

    [Fact]
    public async Task AlreadySoldRejectsAllRequests_AndPreventsNewOrders()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var first = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        await service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id));
        await service.RejectAsync(first.Id, seed.Seller.Id, null, true);
        Assert.All(await db.Orders.ToListAsync(), order => Assert.Equal(OrderStatuses.Rejected, order.Status));
        Assert.Equal(GemListingStatuses.Sold, seed.Gem.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id)));
    }

    [Fact]
    public async Task ExpiryCancelsUnpaidOrderOnce_ReopensGem_AndDoesNotReviveRejectedRequests()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var first = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        var second = await service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id));
        await service.ConfirmAsync(first.Id, seed.Seller.Id, UserRoles.Seller, null);
        (await db.Orders.FindAsync(first.Id))!.PaymentDueAt = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        await service.ExpireUnpaidAsync();
        await service.ExpireUnpaidAsync();
        var cancelled = (await service.GetForBuyerAsync(first.Id, seed.Buyer.Id))!;
        Assert.Equal(OrderStatuses.Cancelled, cancelled.Status);
        Assert.Single(cancelled.StatusHistory, h => h.NewStatus == OrderStatuses.Cancelled);
        Assert.Contains("3 hours", cancelled.StatusHistory.Last().Reason);
        Assert.Equal(OrderStatuses.Rejected, (await service.GetForBuyerAsync(second.Id, seed.Other.Id))!.Status);
        var marketplace = await new MarketplaceService(db).SearchAsync(new() { Page = 1, PageSize = 12 });
        Assert.Contains(marketplace.Items, gem => gem.Id == seed.Gem.Id && gem.IsAvailable);
        Assert.Equal(OrderStatuses.Pending, (await service.CreateAsync(seed.Other.Id, Request(seed.Gem.Id))).Status);
    }

    [Fact]
    public async Task PaymentBeforeDeadlineSucceeds_ExpiryNeverCancelsPaidOrder()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var order = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        await service.ConfirmAsync(order.Id, seed.Seller.Id, UserRoles.Seller, null);
        var payment = await service.PayAsync(order.Id, seed.Buyer.Id, new());
        Assert.Equal(OrderStatuses.Paid, payment.Order.Status);
        (await db.Orders.FindAsync(order.Id))!.PaymentDueAt = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
        await service.ExpireUnpaidAsync();
        Assert.Equal(OrderStatuses.Paid, (await service.GetForBuyerAsync(order.Id, seed.Buyer.Id))!.Status);
        Assert.Single(await db.PaymentTransactions.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PayAsync(order.Id, seed.Buyer.Id, new()));
    }

    [Fact]
    public async Task LatePaymentRejectedEvenBeforeExpiryWorkerRuns()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var order = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        await service.ConfirmAsync(order.Id, seed.Seller.Id, UserRoles.Seller, null);
        (await db.Orders.FindAsync(order.Id))!.PaymentDueAt = DateTime.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.PayAsync(order.Id, seed.Buyer.Id, new()));
        Assert.Empty(await db.PaymentTransactions.ToListAsync());
    }

    [Fact]
    public async Task ReadStateIsScoped_AndStaleReadDoesNotHideNewerMessage()
    {
        await using var db = Database(); var seed = await Seed(db); var service = new OrderService(db);
        var order = await service.CreateAsync(seed.Buyer.Id, Request(seed.Gem.Id));
        await service.MarkMessageReadAsync(order.Id, seed.Seller.Id, true, order.CreatedAt);
        Assert.NotNull((await service.GetForSellerAsync(order.Id, seed.Seller.Id))!.SellerReadAt);
        var approved = await service.ConfirmAsync(order.Id, seed.Seller.Id, UserRoles.Seller, null);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.MarkMessageReadAsync(order.Id, seed.Other.Id, false, approved.BuyerMessageAt!.Value));
        await service.MarkMessageReadAsync(order.Id, seed.Buyer.Id, false, approved.BuyerMessageAt!.Value);
        await service.CancelAsync(order.Id, seed.Buyer.Id, "Buyer cancelled.");
        await service.MarkMessageReadAsync(order.Id, seed.Buyer.Id, false, approved.BuyerMessageAt.Value);
        var latest = (await service.GetForBuyerAsync(order.Id, seed.Buyer.Id))!;
        Assert.True(latest.BuyerReadAt < latest.BuyerMessageAt);
    }
}
