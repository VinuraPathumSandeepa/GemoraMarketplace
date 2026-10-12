using Gemora.API.Controllers;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Gemora.Tests;

public class AdminGemListingTests
{
    [Fact]
    public async Task PendingPurchaseShowsSoldAndAdminDeletePreservesHistory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        context.Users.AddRange(
            new User { Id = sellerId, FullName = "Seller", Email = "seller@example.com", Role = UserRoles.Seller },
            new User { Id = buyerId, FullName = "Buyer", Email = "buyer@example.com", Role = UserRoles.Buyer },
            new User { Id = adminId, FullName = "Admin", Email = "admin@example.com", Role = UserRoles.Admin });
        context.GemListings.Add(new GemListing
        {
            Id = 42, SellerId = sellerId, Title = "Blue Sapphire", GemType = "Sapphire",
            Status = GemListingStatuses.Approved, Price = 1000, Currency = "LKR"
        });
        var orderId = Guid.NewGuid();
        context.Orders.Add(new Order
        {
            Id = orderId, SellerId = sellerId, BuyerId = buyerId, GemListingId = 42,
            Status = OrderStatuses.Pending, Currency = "LKR", TotalAmount = 1000
        });
        context.WishlistItems.Add(new WishlistItem { UserId = buyerId, GemListingId = 42 });
        await context.SaveChangesAsync();

        var service = new GemListingService(context, Mock.Of<IFileStorageService>());
        var marketplace = new MarketplaceService(context);
        Assert.Equal(GemListingStatuses.Sold, (await service.GetMyListingsAsync(sellerId)).Single().Status);
        Assert.Equal(GemListingStatuses.Sold, (await service.GetAllForAdminAsync()).Single().Status);
        Assert.False((await marketplace.GetByIdAsync(42))!.IsAvailable);

        Assert.True(await service.DeleteForAdminAsync(42, adminId));
        Assert.Empty(await service.GetAllForAdminAsync());
        Assert.Empty(await service.GetMyListingsAsync(sellerId));
        Assert.Null(await marketplace.GetByIdAsync(42));
        Assert.Equal(OrderStatuses.Rejected, (await context.Orders.SingleAsync(o => o.Id == orderId)).Status);
        Assert.Equal(adminId, (await context.OrderStatusHistories.SingleAsync()).ChangedByUserId);
        Assert.Equal(GemListingStatuses.Deleted, (await context.GemListings.SingleAsync()).Status);
        Assert.Empty(await context.WishlistItems.ToListAsync());
    }

    [Fact]
    public async Task AdminDeleteKeepsPaidOrderAndItsGemReference()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options);
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        context.Users.AddRange(
            new User { Id = sellerId, FullName = "Seller", Email = "seller2@example.com", Role = UserRoles.Seller },
            new User { Id = buyerId, FullName = "Buyer", Email = "buyer2@example.com", Role = UserRoles.Buyer },
            new User { Id = adminId, FullName = "Admin", Email = "admin2@example.com", Role = UserRoles.Admin });
        context.GemListings.Add(new GemListing
        {
            Id = 73, SellerId = sellerId, Title = "Ruby", Status = GemListingStatuses.Approved,
            Price = 1200, Currency = "LKR"
        });
        var orderId = Guid.NewGuid();
        context.Orders.Add(new Order
        {
            Id = orderId, SellerId = sellerId, BuyerId = buyerId, GemListingId = 73,
            Status = OrderStatuses.Paid, Currency = "LKR", TotalAmount = 1200
        });
        await context.SaveChangesAsync();

        var service = new GemListingService(context, Mock.Of<IFileStorageService>());
        Assert.True(await service.DeleteForAdminAsync(73, adminId));
        var savedOrder = await context.Orders.SingleAsync(o => o.Id == orderId);
        Assert.Equal(OrderStatuses.Paid, savedOrder.Status);
        Assert.Equal(73, savedOrder.GemListingId);
        Assert.Equal(GemListingStatuses.Deleted, (await context.GemListings.SingleAsync()).Status);
    }

    [Fact]
    public void AdminGemListingEndpointsRequireAdminRole()
    {
        var authorization = (AuthorizeAttribute?)Attribute.GetCustomAttribute(
            typeof(AdminGemListingsController), typeof(AuthorizeAttribute));
        Assert.Equal(UserRoles.Admin, authorization?.Roles);
    }
}
