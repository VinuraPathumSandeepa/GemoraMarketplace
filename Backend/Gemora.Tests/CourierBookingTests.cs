using Gemora.Application.DTOs;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Adapters;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gemora.Tests;

public sealed class CourierBookingTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ShipmentService _service;
    private readonly Guid _admin = Guid.NewGuid();
    private readonly Order _order;
    private readonly Shipment _shipment;
    private readonly ShippingPlan _plan;

    public CourierBookingTests()
    {
        _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"CourierBookings_{Guid.NewGuid()}").Options);
        _order = new Order { Status = OrderStatuses.Paid, TotalAmount = 145000, Currency = "LKR" };
        _shipment = new Shipment
        {
            Id = Guid.NewGuid(), OrderId = _order.Id, Status = "ReadyForBooking", Currency = "LKR", DeclaredValue = 145000
        };
        _plan = new ShippingPlan { Id = Guid.NewGuid(), ShipmentId = _shipment.Id, IsApproved = true };
        _context.Orders.Add(_order);
        _context.Shipments.Add(_shipment);
        _context.ShippingPlans.Add(_plan);
        _context.SaveChanges();
        _service = new ShipmentService(_context, new ManualShippingProviderAdapter());
    }

    private RecordCourierBookingRequest Booking() => new()
    {
        CourierName = "Courier", TrackingNumber = "TRACK-100", ExternalShipmentReference = "BOOK-100",
        SelectedService = "Express", BookedAt = DateTime.UtcNow.AddMinutes(-5),
        TrackingUrl = "https://courier.example/track/TRACK-100", ConfirmedWithCourier = true
    };

    [Fact]
    public async Task IssuedBooking_PreservesActualDetails_AndNotifiesBuyer()
    {
        var request = Booking();
        var result = await _service.RecordCourierBookingAsync(_shipment.Id, _admin, "Admin", request);
        Assert.Equal(request.TrackingNumber, result.TrackingNumber);
        Assert.Equal(request.TrackingUrl, result.TrackingUrl);
        Assert.Equal(request.BookedAt, result.BookedAt);
        Assert.Equal("Booked", result.Status);
        Assert.False(result.IsSimulatedBooking);
        Assert.Equal(FulfillmentStatuses.ReadyForDispatch, _order.FulfillmentStatus);
        Assert.NotNull(_order.BuyerMessageAt);
        Assert.Null(_order.DeliveredAt);
        Assert.Equal("CourierBookingRecorded", (await _context.ShipmentTrackingEvents.SingleAsync()).EventType);
    }

    [Fact]
    public async Task Retry_IsIdempotent_AndCannotOverwriteIssuedBooking()
    {
        var request = Booking();
        await _service.RecordCourierBookingAsync(_shipment.Id, _admin, "Admin", request);
        var notifiedAt = _order.BuyerMessageAt;
        await _service.RecordCourierBookingAsync(_shipment.Id, _admin, "Admin", request);
        Assert.Single(await _context.ShipmentTrackingEvents.ToListAsync());
        Assert.Equal(notifiedAt, _order.BuyerMessageAt);
        request.TrackingNumber = "DIFFERENT-TRACKING";
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordCourierBookingAsync(_shipment.Id, _admin, "Admin", request));
        Assert.Equal("TRACK-100", _shipment.TrackingNumber);
    }

    [Fact]
    public async Task ReplacingSimulation_KeepsOriginalReferencesInAudit()
    {
        _shipment.Status = "Booked";
        _shipment.CourierName = "DEMO Gemora Courier Sandbox";
        _shipment.TrackingNumber = "SIM-TRK-OLD";
        _shipment.ExternalShipmentReference = "SIM-BOOK-OLD";
        _context.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(), ShipmentId = _shipment.Id, EventType = "Booked", Description = "Original simulation"
        });
        await _context.SaveChangesAsync();
        var result = await _service.RecordCourierBookingAsync(_shipment.Id, _admin, "Admin", Booking());
        Assert.False(result.IsSimulatedBooking);
        var audit = await _context.ShipmentTrackingEvents.ToListAsync();
        Assert.Equal(2, audit.Count);
        Assert.Contains("SIM-TRK-OLD", audit.Single(e => e.EventType == "CourierBookingRecorded").Description);
        Assert.Contains("SIM-BOOK-OLD", audit.Single(e => e.EventType == "CourierBookingRecorded").Description);
    }

    [Theory]
    [InlineData("Seller")]
    [InlineData("Buyer")]
    public async Task RecordingRequiresAdmin(string role)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.RecordCourierBookingAsync(_shipment.Id, _admin, role, Booking()));
        Assert.Null(_shipment.TrackingNumber);
    }

    [Theory]
    [InlineData("unapproved")]
    [InlineData("unpaid")]
    [InlineData("dispatched")]
    [InlineData("unconfirmed")]
    [InlineData("future")]
    [InlineData("unsafe-url")]
    [InlineData("simulation")]
    [InlineData("empty-tracking")]
    public async Task InvalidBooking_DoesNotSaveOrNotify(string invalid)
    {
        var request = Booking();
        switch (invalid)
        {
            case "unapproved": _plan.IsApproved = false; break;
            case "unpaid": _order.Status = OrderStatuses.Pending; break;
            case "dispatched": _shipment.Status = "InTransit"; break;
            case "unconfirmed": request.ConfirmedWithCourier = false; break;
            case "future": request.BookedAt = DateTime.UtcNow.AddDays(1); break;
            case "unsafe-url": request.TrackingUrl = "javascript:alert(1)"; break;
            case "simulation": request.TrackingNumber = "SIM-TRK-NEW"; break;
            case "empty-tracking": request.TrackingNumber = " "; break;
        }
        await _context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordCourierBookingAsync(_shipment.Id, _admin, "Admin", request));
        Assert.Null(_shipment.TrackingNumber);
        Assert.Null(_order.BuyerMessageAt);
        Assert.Empty(await _context.ShipmentTrackingEvents.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DuplicateTrackingOrReference_IsRejected(bool tracking)
    {
        _context.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(), OrderId = Guid.NewGuid(),
            TrackingNumber = tracking ? "TRACK-100" : "OTHER-TRACK",
            ExternalShipmentReference = tracking ? "OTHER-REF" : "BOOK-100"
        });
        await _context.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordCourierBookingAsync(_shipment.Id, _admin, "Admin", Booking()));
        Assert.Null(_shipment.TrackingNumber);
    }

    [Fact]
    public async Task ManualMode_DoesNotFabricateBookingReferences()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.BookShipmentAsync(_shipment.Id, _admin, "Admin"));
        Assert.Null(_shipment.TrackingNumber);
        Assert.Equal("ReadyForBooking", _shipment.Status);
    }

    [Fact]
    public async Task SimulatedDelivery_LabelsBuyerNotificationAsAcademicSimulation()
    {
        _shipment.Status = "Booked";
        _shipment.TrackingNumber = "SIM-TRK-OLD";
        await _context.SaveChangesAsync();
        await _service.UpdateShipmentStatusAsync(_shipment.Id, _admin, "Admin",
            new UpdateShipmentStatusDto { Status = "InTransit" });
        Assert.Equal("InTransit", _shipment.Status);
        Assert.NotNull(_order.BuyerMessageAt);
        Assert.StartsWith("Academic simulation: ", (await _context.FulfillmentStatusHistories.SingleAsync()).Note);
    }

    [Fact]
    public async Task SandboxBooking_IsClearlyIdentified_AndRetryPreservesReferences()
    {
        var service = new ShipmentService(_context,
            new MockShippingProviderAdapter(NullLogger<MockShippingProviderAdapter>.Instance));
        var first = await service.BookShipmentAsync(_shipment.Id, _admin, "Admin");
        var retry = await service.BookShipmentAsync(_shipment.Id, _admin, "Admin");
        Assert.Equal("Gemora Courier Sandbox", first.CourierName);
        Assert.StartsWith("SIM-TRK-", first.TrackingNumber);
        Assert.StartsWith("SIM-BOOK-", first.ExternalShipmentReference);
        Assert.Equal(first.TrackingNumber, retry.TrackingNumber);
        Assert.Equal(first.ExternalShipmentReference, retry.ExternalShipmentReference);
        Assert.Single(await _context.ShipmentTrackingEvents.ToListAsync());
        Assert.StartsWith("Academic simulation: ", (await _context.FulfillmentStatusHistories.SingleAsync()).Note);
        Assert.True((await service.GetShipmentByIdAsync(_shipment.Id)).IsSimulatedBooking);
    }

    public void Dispose() => _context.Dispose();
}
