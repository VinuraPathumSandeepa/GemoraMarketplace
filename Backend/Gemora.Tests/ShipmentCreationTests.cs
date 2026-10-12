
using System;
using System.Threading.Tasks;

using Gemora.Application.DTOs.Orders;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gemora.Tests;

public class ShipmentCreationTests
{
    [Fact]
    public async Task C3_01_ReadyPaidOrder_HandoverCreatesShipment()
    {
        // ==========================================
        // ARRANGE: Isolated test database
        // ==========================================

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    "Shipment_C3_01_" + Guid.NewGuid())
                .Options;

        await using var db =
            new ApplicationDbContext(options);

        var seller = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Seller",
            Email = "shipping-seller@example.test",
            Role = UserRoles.Seller
        };

        var buyer = new User
        {
            Id = Guid.NewGuid(),
            FullName = "QA Buyer",
            Email = "shipping-buyer@example.test",
            Role = UserRoles.Buyer
        };

        var orderId = Guid.NewGuid();

        // Create an eligible Paid order
        var order = new Order
        {
            Id = orderId,
            BuyerId = buyer.Id,
            SellerId = seller.Id,
            Buyer = buyer,
            Seller = seller,

            TotalAmount = 150000m,
            Currency = "LKR",
            Status = OrderStatuses.Paid,

            FulfillmentStatus =
                FulfillmentStatuses.ReadyForDispatch,

            PaidAt = DateTime.UtcNow,

            DeliveryDetails = new OrderDeliveryDetails
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,

                RecipientName = "QA Customer",
                RecipientPhone = "0700000000",
                AddressLine1 = "15 QA Test Road",

                City = "Colombo",
                District = "Colombo",
                Region = "Western",
                PostalCode = "00100",
                CountryCode = "LK",

                CreatedAt = DateTime.UtcNow
            }
        };

        db.Users.AddRange(seller, buyer);
        db.Orders.Add(order);

        await db.SaveChangesAsync();

        var trackingNumber =
            "QA-C3-01-" + Guid.NewGuid().ToString("N");

        var request = new CreateShipmentRequestDto
        {
            CourierName = "QA Courier Service",
            TrackingNumber = trackingNumber,
            DispatchNote = "Automated shipment test"
        };

        var service = new OrderService(db);

        // ==========================================
        // ACT: Execute actual shipment service
        // ==========================================

        var result =
            await service.HandOverToCourierAsync(
                orderId,
                seller.Id,
                UserRoles.Seller,
                request
            );

        // ==========================================
        // ASSERT: Shipment response
        // ==========================================

        Assert.NotNull(result);

        Assert.Equal(orderId, result.Id);

        Assert.Equal(
            OrderStatuses.Paid,
            result.Status);

        Assert.Equal(
            FulfillmentStatuses.HandedOverToCourier,
            result.FulfillmentStatus);

        Assert.NotNull(result.Shipment);

        Assert.Equal(
            "QA Courier Service",
            result.Shipment!.CourierName);

        Assert.Equal(
            trackingNumber,
            result.Shipment.TrackingNumber);

        Assert.Equal(
            FulfillmentStatuses.HandedOverToCourier,
            result.Shipment.Status);

        // ==========================================
        // ASSERT: Persisted shipment
        // ==========================================

        var savedShipment = await db.Shipments
            .SingleAsync(s => s.OrderId == orderId);

        Assert.NotEqual(
            Guid.Empty,
            savedShipment.Id);

        Assert.Equal(
            trackingNumber,
            savedShipment.TrackingNumber);

        Assert.NotNull(
            savedShipment.HandedOverAt);

        // ==========================================
        // ASSERT: Delivery address locked
        // ==========================================

        var delivery = await db.OrderDeliveryDetails
            .SingleAsync(d => d.OrderId == orderId);

        Assert.NotNull(delivery.LockedAt);

        // ==========================================
        // ASSERT: Fulfillment history
        // ==========================================

        var historyExists =
            await db.FulfillmentStatusHistories
                .AnyAsync(h =>
                    h.OrderId == orderId &&
                    h.NewStatus ==
                        FulfillmentStatuses.HandedOverToCourier);

        Assert.True(historyExists);
    }
}

