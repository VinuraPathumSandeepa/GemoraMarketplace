using Gemora.Application.DTOs.Orders;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Gemora.Tests;

public class PaymentConfirmationTests
{
    [Fact]
    public async Task PaymentIntentConfirmationMarksTransactionAndOrderPaid()
    {
        await using var db = Database();
        var seed = await Seed(db);
        var service = new OrderService(db);

        var order = await service.CreateAsync(
            seed.Buyer.Id,
            Request(seed.Gem.Id));

        await service.ConfirmAsync(
            order.Id,
            seed.Seller.Id,
            UserRoles.Seller,
            null);

        var intent = await service.CreatePaymentIntentAsync(
            order.Id,
            seed.Buyer.Id,
            new CreatePaymentIntentRequestDto());

        Assert.Equal(PaymentStatuses.Pending, intent.Status);
        Assert.Equal(100000m, intent.OrderTotalAmount);
        Assert.Equal(0.60m, intent.AdvancePercentage);
        Assert.Equal(60000m, intent.Amount);
        Assert.Equal(40000m, intent.RemainingAmount);
        Assert.Single(await db.PaymentTransactions.ToListAsync());

        var payment = await service.ConfirmPaymentAsync(
            order.Id,
            seed.Buyer.Id,
            new ConfirmPaymentRequestDto
            {
                PaymentIntentId = intent.PaymentIntentId,
                PaymentMethodToken = "demo_card_test"
            });

        Assert.Equal(PaymentStatuses.Succeeded, payment.Status);
        Assert.Equal(60000m, payment.Amount);
        Assert.Equal(40000m, payment.RemainingAmount);
        Assert.Equal(OrderStatuses.Paid, payment.Order.Status);
        Assert.NotNull(payment.Order.PaidAt);
        Assert.Equal(FulfillmentStatuses.Pending, payment.Order.FulfillmentStatus);
        Assert.Equal(
            PaymentStatuses.Succeeded,
            (await db.PaymentTransactions.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(23150, 14000)]
    [InlineData(100000, 60000)]
    public void AdvanceAmountIsSixtyPercentRoundedToNearestThousand(
        decimal orderTotal,
        decimal expectedAdvance)
    {
        Assert.Equal(
            expectedAdvance,
            PaymentTerms.CalculateAdvanceAmount(orderTotal));
    }

    [Fact]
    public async Task DeclinedCardDoesNotMarkOrderPaid()
    {
        await using var db = Database();
        var seed = await Seed(db);
        var service = new OrderService(db);

        var order = await service.CreateAsync(
            seed.Buyer.Id,
            Request(seed.Gem.Id));

        await service.ConfirmAsync(
            order.Id,
            seed.Seller.Id,
            UserRoles.Seller,
            null);

        var intent = await service.CreatePaymentIntentAsync(
            order.Id,
            seed.Buyer.Id,
            new CreatePaymentIntentRequestDto());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmPaymentAsync(
                order.Id,
                seed.Buyer.Id,
                new ConfirmPaymentRequestDto
                {
                    PaymentIntentId = intent.PaymentIntentId,
                    PaymentMethodToken = "demo_card_decline"
                }));

        var storedOrder = await db.Orders.SingleAsync(o => o.Id == order.Id);
        var storedPayment = await db.PaymentTransactions.SingleAsync();

        Assert.Equal(OrderStatuses.Confirmed, storedOrder.Status);
        Assert.Null(storedOrder.PaidAt);
        Assert.Equal(PaymentStatuses.Failed, storedPayment.Status);
    }

    private static ApplicationDbContext Database()
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);
    }

    private static async Task<(User Seller, User Buyer, GemListing Gem)> Seed(
        ApplicationDbContext db)
    {
        var seller = new User
        {
            FullName = "Seller",
            Email = "seller-payment@test.example",
            Role = UserRoles.Seller
        };

        var buyer = new User
        {
            FullName = "Buyer",
            Email = "buyer-payment@test.example",
            Role = UserRoles.Buyer
        };

        var gem = new GemListing
        {
            Title = "Payment test sapphire",
            SellerId = seller.Id,
            Seller = seller,
            Status = GemListingStatuses.Approved,
            Price = 100000,
            Currency = "LKR"
        };

        db.AddRange(seller, buyer, gem);
        await db.SaveChangesAsync();

        return (seller, buyer, gem);
    }

    private static CreateOrderRequestDto Request(int gemId)
    {
        return new CreateOrderRequestDto
        {
            GemListingId = gemId,
            DeliveryDetails = new DeliveryDetailsRequestDto
            {
                RecipientName = "Buyer",
                RecipientPhone = "0771234567",
                AddressLine1 = "1 Main Street",
                City = "Colombo",
                District = "Colombo",
                Region = "Western",
                PostalCode = "00100",
                CountryCode = "LK"
            }
        };
    }
}
