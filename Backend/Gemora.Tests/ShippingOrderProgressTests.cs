using Gemora.Application.DTOs;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Gemora.Tests;

public class ShippingOrderProgressTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _buyerId = Guid.NewGuid();
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();
    private readonly Shipment _shipment;
    private readonly Order _order;
    private readonly ShippingPlan _plan;
    private readonly ShipmentService _shipments;
    private readonly OrderService _orders;

    public ShippingOrderProgressTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ShippingProgress_{Guid.NewGuid()}").Options);
        _context.Users.AddRange(
            new User { Id = _buyerId, Email = "buyer@test.com", FullName = "Buyer", Role = "Buyer" },
            new User { Id = _sellerId, Email = "seller@test.com", FullName = "Seller", Role = "Seller" },
            new User { Id = _adminId, Email = "admin@test.com", FullName = "Admin", Role = "Admin" });
        var earlier = DateTime.UtcNow.AddMinutes(-10);
        _order = new Order
        {
            BuyerId = _buyerId, SellerId = _sellerId, TotalAmount = 145000,
            Currency = "LKR", Status = OrderStatuses.Paid,
            BuyerMessageAt = earlier, BuyerReadAt = earlier,
            DeliveryDetails = new OrderDeliveryDetails
            {
                Id = Guid.NewGuid(), RecipientName = "Buyer", RecipientPhone = "0771234567",
                AddressLine1 = "Delivery address", City = "Colombo", Region = "Western"
            },
            StatusHistory = new List<OrderStatusHistory>
            {
                new() { NewStatus = OrderStatuses.Paid, Reason = "Payment completed.", CreatedAt = earlier }
            }
        };
        _shipment = new Shipment
        {
            Id = Guid.NewGuid(), OrderId = _order.Id, BuyerId = _buyerId, SellerId = _sellerId,
            Currency = "LKR", DeclaredValue = 145000, Status = "PlanGenerated"
        };
        _plan = new ShippingPlan
        {
            Id = Guid.NewGuid(), ShipmentId = _shipment.Id, RecommendedServiceType = "Express Insured"
        };
        _context.Orders.Add(_order);
        _context.Shipments.Add(_shipment);
        _context.ShippingPlans.Add(_plan);
        _context.SaveChanges();

        var provider = new Mock<IShippingProviderAdapter>();
        provider.Setup(p => p.BookShipmentAsync(It.IsAny<CourierBookingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierBookingResult
            {
                Success = true, CourierName = "Courier", TrackingNumber = "TRACK-123",
                ExternalShipmentReference = "BOOK-123", SelectedService = "Express Insured"
            });
        _shipments = new ShipmentService(_context, provider.Object);
        _orders = new OrderService(_context);
    }

    [Fact]
    public async Task Approval_NotifiesOnlyBuyer_WithoutClaimingDelivery()
    {
        await new ShippingAgentService(_context).ApproveShippingPlanAsync(_shipment.Id, _adminId, "Private admin note");
        _context.ChangeTracker.Clear();
        var order = Assert.Single(await _orders.GetMyOrdersAsync(_buyerId));
        Assert.Equal(OrderStatuses.Paid, order.Status);
        Assert.Equal(FulfillmentStatuses.Preparing, order.FulfillmentStatus);
        Assert.True(order.BuyerMessageAt > order.BuyerReadAt);
        Assert.Null(order.DeliveredAt);
        Assert.Equal(ShipmentOrderProgress.ApprovalMessage, Assert.Single(order.FulfillmentHistory).Note);
        Assert.Empty(await _orders.GetMyOrdersAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _orders.CompleteAsync(order.Id, _buyerId, null));
        Assert.Equal("PlanGenerated", (await _context.ShipmentTrackingEvents.SingleAsync()).PreviousState);
    }

    [Fact]
    public async Task DeliveryWorkflow_UpdatesBuyerTimeline_AndAllowsOwnedReceiptConfirmation()
    {
        await new ShippingAgentService(_context).ApproveShippingPlanAsync(_shipment.Id, _adminId, null);
        await _shipments.BookShipmentAsync(_shipment.Id, _adminId, "Admin");
        var bookingMessageAt = _order.BuyerMessageAt!.Value;
        await _orders.MarkMessageReadAsync(_order.Id, _buyerId, false, bookingMessageAt);
        Assert.Equal(FulfillmentStatuses.ReadyForDispatch, _order.FulfillmentStatus);

        foreach (var status in new[] { "PickedUp", "InTransit", "OutForDelivery", "Delivered" })
            await _shipments.UpdateShipmentStatusAsync(_shipment.Id, _adminId, "Admin",
                new UpdateShipmentStatusDto { Status = status });

        _context.ChangeTracker.Clear();
        var delivered = Assert.Single(await _orders.GetMyOrdersAsync(_buyerId));
        Assert.Equal(FulfillmentStatuses.Delivered, delivered.FulfillmentStatus);
        Assert.Equal("Delivered", delivered.Shipment!.Status);
        Assert.NotNull(delivered.DeliveredAt);
        Assert.NotNull(delivered.HandedOverAt);
        Assert.True(delivered.DeliveryDetails!.IsLocked);
        Assert.Equal(delivered.HandedOverAt, delivered.DeliveryDetails.LockedAt);
        Assert.Equal(OrderStatuses.Paid, delivered.Status);
        Assert.True(delivered.BuyerMessageAt > delivered.BuyerReadAt);
        Assert.Contains("Confirm receipt", delivered.FulfillmentHistory.Last().Note);
        await _orders.MarkMessageReadAsync(_order.Id, _buyerId, false, bookingMessageAt);
        Assert.Equal(bookingMessageAt, (await _context.Orders.FindAsync(_order.Id))!.BuyerReadAt);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _orders.CompleteAsync(_order.Id, Guid.NewGuid(), null));
        var completed = await _orders.CompleteAsync(_order.Id, _buyerId, null);
        Assert.Equal(OrderStatuses.Completed, completed.Status);
        Assert.Equal(FulfillmentStatuses.Delivered, completed.FulfillmentStatus);
    }

    [Fact]
    public async Task InvalidOrUnauthorizedDelivery_DoesNotChangeBuyerProgress()
    {
        await new ShippingAgentService(_context).ApproveShippingPlanAsync(_shipment.Id, _adminId, null);
        var messageAt = _order.BuyerMessageAt;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _shipments.UpdateShipmentStatusAsync(_shipment.Id, _adminId, "Admin",
                new UpdateShipmentStatusDto { Status = "Delivered" }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _shipments.UpdateShipmentStatusAsync(_shipment.Id, _sellerId, "Seller",
                new UpdateShipmentStatusDto { Status = "Delivered" }));
        Assert.Equal(messageAt, _order.BuyerMessageAt);
        Assert.Equal(FulfillmentStatuses.Preparing, _order.FulfillmentStatus);
        Assert.Null(_order.DeliveredAt);
    }

    [Fact]
    public async Task ExistingApproval_IsRestoredOnce_AndDoesNotRegressDelivery()
    {
        _plan.IsApproved = true;
        _plan.ApprovedAt = DateTime.UtcNow.AddMinutes(-5);
        _plan.ApprovedBy = _adminId;
        _shipment.Status = "InTransit";
        _shipment.ShippedAt = DateTime.UtcNow.AddMinutes(-2);
        await _context.SaveChangesAsync();
        await ShipmentOrderProgress.ReconcileExistingAsync(_context);
        Assert.Equal(FulfillmentStatuses.InTransit, _order.FulfillmentStatus);
        Assert.Equal(2, await _context.FulfillmentStatusHistories.CountAsync());
        var messageAt = _order.BuyerMessageAt!.Value;
        await _orders.MarkMessageReadAsync(_order.Id, _buyerId, false, messageAt);
        await ShipmentOrderProgress.ReconcileExistingAsync(_context);
        Assert.Equal(messageAt, _order.BuyerMessageAt);
        Assert.Equal(2, await _context.FulfillmentStatusHistories.CountAsync());

        _order.FulfillmentStatus = FulfillmentStatuses.Delivered;
        await _context.SaveChangesAsync();
        await ShipmentOrderProgress.ReconcileExistingAsync(_context);
        Assert.Equal(FulfillmentStatuses.Delivered, _order.FulfillmentStatus);
        Assert.Equal(messageAt, _order.BuyerMessageAt);
    }

    [Fact]
    public async Task CompletedOrders_ArePreservedOnReconciliation()
    {
        _order.Status = OrderStatuses.Completed;
        _order.FulfillmentStatus = FulfillmentStatuses.Delivered;
        _plan.IsApproved = true;
        _shipment.Status = "Delivered";
        await _context.SaveChangesAsync();
        var messageAt = _order.BuyerMessageAt;
        await ShipmentOrderProgress.ReconcileExistingAsync(_context);
        Assert.Equal(OrderStatuses.Completed, _order.Status);
        Assert.Equal(messageAt, _order.BuyerMessageAt);
        Assert.Empty(await _context.FulfillmentStatusHistories.ToListAsync());
    }

    [Fact]
    public async Task ExistingDeliveredShipment_RestoresTimeline_AndAllowsReceiptConfirmation()
    {
        _plan.IsApproved = true;
        _plan.ApprovedAt = DateTime.UtcNow.AddHours(-3);
        _shipment.Status = "Delivered";
        _shipment.ShippedAt = DateTime.UtcNow.AddHours(-2);
        _shipment.DeliveredAt = DateTime.UtcNow.AddHours(-1);
        await _context.SaveChangesAsync();
        await ShipmentOrderProgress.ReconcileExistingAsync(_context);
        Assert.Equal(FulfillmentStatuses.Delivered, _order.FulfillmentStatus);
        Assert.Equal(_shipment.DeliveredAt, _order.DeliveredAt);
        var notifiedAt = _order.BuyerMessageAt;
        await ShipmentOrderProgress.ReconcileExistingAsync(_context);
        Assert.Equal(notifiedAt, _order.BuyerMessageAt);
        Assert.Equal(2, await _context.FulfillmentStatusHistories.CountAsync());
        Assert.Equal(OrderStatuses.Completed, (await _orders.CompleteAsync(_order.Id, _buyerId, null)).Status);
    }

    public void Dispose() => _context.Dispose();
}
