using System.Data;

using Gemora.Application.DTOs.Orders;

using Gemora.Application.Interfaces;

using Gemora.Domain.Constants;

using Gemora.Domain.Entities;

using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public partial class OrderService : IOrderService

{

    private readonly ApplicationDbContext _context;
    private readonly IPaymentGateway _paymentGateway;

    public OrderService(
        ApplicationDbContext context,
        IPaymentGateway? paymentGateway = null)

    {

        _context = context;
        _paymentGateway = paymentGateway ?? new SandboxPaymentGateway();

    }

    // =========================================================

    // CREATE ORDER

    // =========================================================

    public async Task<OrderResponseDto> CreateAsync(

        Guid buyerId,

        CreateOrderRequestDto dto)

    {

        var buyer = await _context.Users

            .AsNoTracking()

            .FirstOrDefaultAsync(u =>

                u.Id == buyerId &&

                u.Role == UserRoles.Buyer)

            ?? throw new UnauthorizedAccessException(

                "Only Buyers can create orders.");

        await using var transaction =

            await _context.Database.BeginTransactionAsync(

                IsolationLevel.Serializable);

        try

        {

            var listing =

                await _context.GemListings

                    .FirstOrDefaultAsync(g =>

                        g.Id == dto.GemListingId)

                ?? throw new KeyNotFoundException(

                    "Gem listing was not found.");

            if (listing.Status != GemListingStatuses.Approved)

            {

                throw new InvalidOperationException(

                    "Only approved gemstones can be purchased.");

            }

            if (listing.SellerId == buyerId)

            {

                throw new InvalidOperationException(

                    "You cannot purchase your own listing.");

            }

            // =====================================================

            // UNIQUE GEMSTONE AVAILABILITY

            // =====================================================

            var hasActiveOrder =

                await _context.Orders

                    .AnyAsync(o =>

                        o.GemListingId == listing.Id &&

                        o.Status != OrderStatuses.Cancelled &&

                        o.Status != OrderStatuses.Refunded &&

                        o.Status != OrderStatuses.Failed &&
                        o.Status != OrderStatuses.Rejected &&
                        (o.Status != OrderStatuses.Pending || o.BuyerId == buyerId));

            if (hasActiveOrder)

            {

                throw new InvalidOperationException(

                    "This gemstone is not currently available for purchase.");

            }

            // =====================================================

            // DELIVERY PAYLOAD

            //

            // New frontend:

            //   DeliveryDetails object

            //

            // Old frontend:

            //   ShippingAddress /

            //   ShippingRegion /

            //   ShippingCountryCode

            //

            // We temporarily support both.

            // =====================================================

            var delivery =

                dto.DeliveryDetails;

            var hasStructuredDelivery =

                HasAnyStructuredDeliveryValue(

                    delivery);

            if (hasStructuredDelivery)

            {

                ValidateDeliveryDetails(

                    delivery!);

            }

            else

            {

                ValidateLegacyShipping(

                    dto);

            }

            var legacyAddress =

                hasStructuredDelivery

                    ? BuildLegacyAddress(

                        delivery!)

                    : dto.ShippingAddress!

                        .Trim();

            var legacyRegion =

                hasStructuredDelivery

                    ? delivery!.Region.Trim()

                    : dto.ShippingRegion!

                        .Trim();

            var legacyCountry =

                hasStructuredDelivery

                    ? delivery!.CountryCode

                        .Trim()

                        .ToUpperInvariant()

                    : dto.ShippingCountryCode!

                        .Trim()

                        .ToUpperInvariant();

            var now =

                DateTime.UtcNow;

            // =====================================================

            // CREATE ORDER

            // =====================================================

            var order =

                new Order

                {

                    Id =

                        Guid.NewGuid(),

                    BuyerId =

                        buyer.Id,

                    SellerId =

                        listing.SellerId,

                    GemListingId =

                        listing.Id,

                    TotalAmount =

                        listing.Price,

                    Currency =

                        listing.Currency,

                    Status =

                        OrderStatuses.Pending,

                    FulfillmentStatus =

                        FulfillmentStatuses.Pending,

                    // Keep legacy fields for compatibility.

                    ShippingAddress =

                        legacyAddress,

                    ShippingRegion =

                        legacyRegion,

                    ShippingCountryCode =

                        legacyCountry,

                    CreatedAt =

                        now

                };

            _context.Orders.Add(

                order);

            await _context

                .SaveChangesAsync();

            // =====================================================

            // STRUCTURED DELIVERY SNAPSHOT

            // =====================================================

            if (hasStructuredDelivery)

            {

                var deliveryDetails =

                    CreateDeliveryEntity(

                        order.Id,

                        buyerId,

                        delivery!,

                        now);

                _context.OrderDeliveryDetails

                    .Add(deliveryDetails);

                await _context

                    .SaveChangesAsync();

            }

            // =====================================================

            // ORDER HISTORY

            // =====================================================

            var history =

                new OrderStatusHistory

                {

                    OrderId =

                        order.Id,

                    PreviousStatus =

                        null,

                    NewStatus =

                        OrderStatuses.Pending,

                    ChangedByUserId =

                        buyerId,

                    Reason =

                        "Order created by Buyer.",

                    CreatedAt =

                        now

                };

            _context.OrderStatusHistories

                .Add(history);

            await _context

                .SaveChangesAsync();

            await transaction

                .CommitAsync();

            return (

                await LoadOrderAsync(

                    order.Id)

            )!;

        }

        catch

        {

            await transaction

                .RollbackAsync();

            throw;

        }

    }

    private async Task ChangeFulfillmentStatusAsync(

        Order order,

        string newStatus,

        Guid actorId,

        string? note)

    {

        var previousStatus =

            order.FulfillmentStatus;

        order.FulfillmentStatus =

            newStatus;

        order.UpdatedAt =

            DateTime.UtcNow;

        var history =

            new FulfillmentStatusHistory

            {

                OrderId =

                    order.Id,

                PreviousStatus =

                    previousStatus,

                NewStatus =

                    newStatus,

                ChangedByUserId =

                    actorId,

                Note =

                    string.IsNullOrWhiteSpace(

                        note)

                        ? null

                        : note.Trim(),

                CreatedAt =

                    DateTime.UtcNow

            };

        _context

            .FulfillmentStatusHistories

            .Add(history);

        await _context

            .SaveChangesAsync();

    }

    // =========================================================

    // BUYER ORDER

    // =========================================================

    public Task<OrderResponseDto?> GetForBuyerAsync(

        Guid id,

        Guid buyerId)

    {

        return LoadOrderAsync(

            id,

            o => o.BuyerId == buyerId);

    }

    // =========================================================

    // BUYER ORDERS

    // =========================================================

    public async Task<List<OrderResponseDto>>
    GetMyOrdersAsync(Guid buyerId)
{
    var orders =
        await _context.Orders

            .AsNoTracking()

            // Important:
            // Multiple collection relationships exist:
            //
            // StatusHistory
            // FulfillmentStatusHistory
            //
            // Split queries prevent one huge joined
            // SQL query and avoid collection projection
            // problems / cartesian explosion.
            .AsSplitQuery()

            .Where(o =>
                o.BuyerId == buyerId)

            .Include(o =>
                o.Buyer)

            .Include(o =>
                o.Seller)

            .Include(o =>
                o.GemListing)

            .Include(o =>
                o.DeliveryDetails)

            .Include(o =>
                o.Shipment)

            .Include(o =>
                o.StatusHistory)
                .ThenInclude(h =>
                    h.ChangedByUser)

            .Include(o =>
                o.FulfillmentStatusHistory)
                .ThenInclude(h =>
                    h.ChangedByUser)

            .OrderByDescending(o =>
                o.CreatedAt)

            .ToListAsync();


    return orders
        .Select(MapOrderToDto)
        .ToList();
}
    // =========================================================

    // SELLER ORDER

    // =========================================================

    public Task<OrderResponseDto?> GetForSellerAsync(

        Guid id,

        Guid sellerId)

    {

        return LoadOrderAsync(

            id,

            o => o.SellerId == sellerId);

    }

    // =========================================================

    // SELLER ORDERS

    // =========================================================

    public async Task<List<OrderResponseDto>>

        GetSellerOrdersAsync(Guid sellerId)

    {

        return await LoadOrdersQuery()

            .Where(o =>

                o.SellerId == sellerId)

            .OrderByDescending(o =>

                o.CreatedAt)

            .Select(OrderProjection())

            .ToListAsync();

    }

    // =========================================================

    // ADMIN ORDERS

    // =========================================================

    public async Task<List<OrderResponseDto>>

        GetAllForAdminAsync()

    {

        return await LoadOrdersQuery()

            .OrderByDescending(o =>

                o.CreatedAt)

            .Select(OrderProjection())

            .ToListAsync();

    }

    // =========================================================

    // CANCEL ORDER

    // =========================================================

    public async Task<OrderResponseDto> CancelAsync(

        Guid id,

        Guid buyerId,

        string? reason)

    {

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order =

            await _context.Orders

                .FirstOrDefaultAsync(o =>

                    o.Id == id &&

                    o.BuyerId == buyerId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        if (

            order.Status != OrderStatuses.Pending &&

            order.Status != OrderStatuses.Confirmed &&

            order.Status != OrderStatuses.AwaitingPayment)

        {

            throw new InvalidOperationException(

                $"Order cannot be cancelled from status '{order.Status}'.");

        }

        await ChangeStatusAsync(

            order,

            OrderStatuses.Cancelled,

            buyerId,

            reason ??

            "Cancelled by Buyer.");

        await transaction.CommitAsync();
        return (

            await LoadOrderAsync(id)

        )!;

    }

    // =========================================================

    // CONFIRM ORDER

    // =========================================================

    public async Task<OrderResponseDto> ConfirmAsync(

        Guid id,

        Guid actorId,

        string actorRole,

        string? reason)

    {

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await _context.Orders.Include(o => o.GemListing).FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException("Order was not found.");
        ValidateSellerOrAdmin(order, actorId, actorRole);
        if (order.Status != OrderStatuses.Pending)
            throw new InvalidOperationException("Only pending orders can be approved.");
        if (order.GemListing?.Status != GemListingStatuses.Approved)
            throw new InvalidOperationException("This gemstone is no longer available.");
        if (await _context.Orders.AnyAsync(o => o.GemListingId == order.GemListingId &&
            o.Id != id && o.Status != OrderStatuses.Pending && o.Status != OrderStatuses.Rejected &&
            o.Status != OrderStatuses.Cancelled && o.Status != OrderStatuses.Refunded && o.Status != OrderStatuses.Failed))
            throw new InvalidOperationException("Another order has already been approved for this gemstone.");
        order.PaymentDueAt = DateTime.UtcNow.AddHours(3);
        await ChangeStatusAsync(order, OrderStatuses.Confirmed, actorId,
            "Seller approved your order. Complete payment within 3 hours.");
        var others = await _context.Orders.Where(o => o.GemListingId == order.GemListingId &&
            o.Id != id && o.Status == OrderStatuses.Pending).ToListAsync();
        foreach (var other in others)
            await ChangeStatusAsync(other, OrderStatuses.Rejected, actorId,
                "Already sold / allocated to another buyer — the seller approved another order.");
        await transaction.CommitAsync();
        return (await LoadOrderAsync(id))!;
    }

    // =========================================================

    // UPDATE DELIVERY DETAILS

    //

    // IMPORTANT:

    // Payment does NOT lock the address.

    //

    // Buyer can edit until the gemstone has

    // physically been handed over to the courier.

    // =========================================================

    public async Task<OrderResponseDto>

        UpdateDeliveryDetailsAsync(

            Guid orderId,

            Guid buyerId,

            DeliveryDetailsRequestDto dto)

    {

        var order =

            await _context.Orders

                .Include(o =>

                    o.DeliveryDetails)

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId &&

                    o.BuyerId == buyerId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        if (

            order.Status ==

                OrderStatuses.Cancelled ||

            order.Status ==

                OrderStatuses.Refunded ||

            order.Status == OrderStatuses.Rejected ||
            order.Status ==

                OrderStatuses.Failed ||

            order.Status ==

                OrderStatuses.Completed)

        {

            throw new InvalidOperationException(

                "Delivery details cannot be changed for this order.");

        }

        if (IsDeliveryLocked(

                order))

        {

            throw new InvalidOperationException(

                "Delivery details can no longer be changed because the gemstone has already been handed over to the courier.");

        }

        ValidateDeliveryDetails(

            dto);

        var now =

            DateTime.UtcNow;

        var details =

            order.DeliveryDetails;

        if (details == null)

        {

            details =

                new OrderDeliveryDetails

                {

                    Id =

                        Guid.NewGuid(),

                    OrderId =

                        order.Id,

                    CreatedAt =

                        now

                };

            _context.OrderDeliveryDetails

                .Add(details);

        }

        ApplyDeliveryDetails(

            details,

            dto,

            buyerId,

            now);

        // Keep old columns synchronized

        // until legacy code is removed.

        order.ShippingAddress =

            BuildLegacyAddress(

                dto);

        order.ShippingRegion =

            dto.Region.Trim();

        order.ShippingCountryCode =

            dto.CountryCode

                .Trim()

                .ToUpperInvariant();

        order.UpdatedAt =

            now;

        await _context

            .SaveChangesAsync();

        return (

            await LoadOrderAsync(

                order.Id)

        )!;

    }

    // =========================================================

    // PAYMENT

    // =========================================================

    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
        Guid orderId,
        Guid buyerId,
        CreatePaymentIntentRequestDto dto)
    {
        ValidateCardPaymentMethod(dto.PaymentMethod);

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var committed = false;

        try
        {
            var order =
                await _context.Orders
                    .Include(o => o.DeliveryDetails)
                    .FirstOrDefaultAsync(o =>
                        o.Id == orderId &&
                        o.BuyerId == buyerId)
                ?? throw new KeyNotFoundException(
                    "Order was not found.");

            await ValidatePaymentEligibilityAsync(order);

            var existingPayment =
                await _context.PaymentTransactions
                    .Where(p =>
                        p.OrderId == order.Id &&
                        p.Status == PaymentStatuses.Pending)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                await transaction.CommitAsync();
                committed = true;

                return new PaymentIntentResponseDto
                {
                    PaymentIntentId = existingPayment.ExternalReference,
                    OrderId = existingPayment.OrderId,
                    Provider = existingPayment.Provider,
                    Amount = existingPayment.Amount,
                    Currency = existingPayment.Currency,
                    Status = existingPayment.Status,
                    ExpiresAt = order.PaymentDueAt!.Value
                };
            }

            var gatewayIntent =
                await _paymentGateway.CreatePaymentIntentAsync(
                    order.Id,
                    order.TotalAmount,
                    order.Currency);

            var now = DateTime.UtcNow;
            var payment = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Provider = gatewayIntent.Provider,
                ExternalReference = gatewayIntent.PaymentIntentId,
                Amount = gatewayIntent.Amount,
                Currency = gatewayIntent.Currency,
                Status = PaymentStatuses.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.PaymentTransactions.Add(payment);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            committed = true;

            return new PaymentIntentResponseDto
            {
                PaymentIntentId = payment.ExternalReference,
                OrderId = payment.OrderId,
                Provider = payment.Provider,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                ExpiresAt = order.PaymentDueAt!.Value
            };
        }
        catch
        {
            if (!committed)
            {
                await transaction.RollbackAsync();
            }

            throw;
        }
    }

    public async Task<PaymentResponseDto> ConfirmPaymentAsync(
        Guid orderId,
        Guid buyerId,
        ConfirmPaymentRequestDto dto)
    {
        ValidateCardPaymentMethod(dto.PaymentMethod);

        if (string.IsNullOrWhiteSpace(dto.PaymentIntentId))
        {
            throw new InvalidOperationException(
                "A payment intent is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.PaymentMethodToken))
        {
            throw new InvalidOperationException(
                "A payment method token is required.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var committed = false;

        try
        {
            var order =
                await _context.Orders
                    .Include(o => o.DeliveryDetails)
                    .FirstOrDefaultAsync(o =>
                        o.Id == orderId &&
                        o.BuyerId == buyerId)
                ?? throw new KeyNotFoundException(
                    "Order was not found.");

            var payment =
                await _context.PaymentTransactions
                    .FirstOrDefaultAsync(p =>
                        p.OrderId == order.Id &&
                        p.ExternalReference == dto.PaymentIntentId.Trim());

            if (payment == null)
            {
                throw new KeyNotFoundException(
                    "Payment intent was not found for this order.");
            }

            if (payment.Status == PaymentStatuses.Succeeded)
            {
                await transaction.CommitAsync();
                committed = true;

                return new PaymentResponseDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Provider = payment.Provider,
                    ExternalReference = payment.ExternalReference,
                    Amount = payment.Amount,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    CreatedAt = payment.CreatedAt,
                    Order = (await LoadOrderAsync(order.Id))!
                };
            }

            if (payment.Status != PaymentStatuses.Pending)
            {
                throw new InvalidOperationException(
                    "This payment intent is no longer available.");
            }

            await ValidatePaymentEligibilityAsync(order);

            if (payment.Amount != order.TotalAmount ||
                !string.Equals(
                    payment.Currency,
                    order.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The payment amount no longer matches the order.");
            }

            var confirmation =
                await _paymentGateway.ConfirmPaymentAsync(
                    payment.ExternalReference,
                    dto.PaymentMethodToken.Trim(),
                    payment.Amount,
                    payment.Currency);

            var now = DateTime.UtcNow;

            if (!confirmation.Succeeded)
            {
                payment.Status = PaymentStatuses.Failed;
                payment.UpdatedAt = now;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                committed = true;

                throw new InvalidOperationException(
                    confirmation.FailureReason ??
                    "The card payment was declined.");
            }

            payment.Status = PaymentStatuses.Succeeded;
            payment.UpdatedAt = now;

            order.PaidAt = now;
            order.FulfillmentStatus = FulfillmentStatuses.Pending;

            await ChangeStatusAsync(
                order,
                OrderStatuses.Paid,
                buyerId,
                "Payment completed by Buyer using Card.");

            await transaction.CommitAsync();
            committed = true;

            return new PaymentResponseDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Provider = payment.Provider,
                ExternalReference = payment.ExternalReference,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                CreatedAt = payment.CreatedAt,
                Order = (await LoadOrderAsync(order.Id))!
            };
        }
        catch
        {
            if (!committed)
            {
                await transaction.RollbackAsync();
            }

            throw;
        }
    }

    // Backward-compatible service wrapper for existing internal callers.
    public async Task<PaymentResponseDto> PayAsync(
        Guid orderId,
        Guid buyerId,
        CreatePaymentRequestDto dto)
    {
        var intent =
            await CreatePaymentIntentAsync(
                orderId,
                buyerId,
                new CreatePaymentIntentRequestDto
                {
                    PaymentMethod = dto.PaymentMethod
                });

        return await ConfirmPaymentAsync(
            orderId,
            buyerId,
            new ConfirmPaymentRequestDto
            {
                PaymentIntentId = intent.PaymentIntentId,
                PaymentMethod = dto.PaymentMethod,
                PaymentMethodToken =
                    SandboxPaymentGateway.LegacyPaymentMethodToken
            });
    }

    private async Task ValidatePaymentEligibilityAsync(Order order)
    {
        if (order.PaymentDueAt == null ||
            order.PaymentDueAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "The payment window has expired. This order can no longer be paid.");
        }

        if (await _context.Orders.AnyAsync(o =>
                o.GemListingId == order.GemListingId &&
                o.Id != order.Id &&
                o.Status != OrderStatuses.Pending &&
                o.Status != OrderStatuses.Rejected &&
                o.Status != OrderStatuses.Cancelled &&
                o.Status != OrderStatuses.Refunded &&
                o.Status != OrderStatuses.Failed))
        {
            throw new InvalidOperationException(
                "This gemstone has another approved or paid order. Payment is blocked until the conflicting orders are resolved.");
        }

        if (order.Status != OrderStatuses.Confirmed &&
            order.Status != OrderStatuses.AwaitingPayment)
        {
            throw new InvalidOperationException(
                $"Payment cannot be completed from status '{order.Status}'.");
        }

        if (order.DeliveryDetails == null)
        {
            throw new InvalidOperationException(
                "Complete your delivery details before making payment.");
        }

        ValidateStoredDeliveryDetails(order.DeliveryDetails);

        if (await _context.PaymentTransactions.AnyAsync(p =>
                p.OrderId == order.Id &&
                p.Status == PaymentStatuses.Succeeded) ||
            order.PaidAt.HasValue ||
            order.Status == OrderStatuses.Paid)
        {
            throw new InvalidOperationException(
                "This order has already been paid.");
        }
    }

    private static void ValidateCardPaymentMethod(string? paymentMethod)
    {
        if (!string.Equals(
                paymentMethod?.Trim(),
                "Card",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Only card payments are currently supported.");
        }
    }

    // SELLER STARTS PREPARING

    // =========================================================

    public async Task<OrderResponseDto>

        StartPreparingAsync(

            Guid orderId,

            Guid actorId,

            string actorRole,

            string? reason)

    {

        var order =

            await _context.Orders

                .Include(o =>

                    o.DeliveryDetails)

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        ValidateSellerOrAdmin(

            order,

            actorId,

            actorRole);

        if (

            order.Status !=

            OrderStatuses.Paid)

        {

            throw new InvalidOperationException(

                "Only paid orders can enter preparation.");

        }

        if (

            order.DeliveryDetails ==

            null)

        {

            throw new InvalidOperationException(

                "Delivery details are missing.");

        }

        ValidateStoredDeliveryDetails(

            order.DeliveryDetails);

        if (

            order.FulfillmentStatus !=

            FulfillmentStatuses.Pending)

        {

            throw new InvalidOperationException(

                $"Order cannot start preparation from fulfillment status '{order.FulfillmentStatus}'.");

        }

        await ChangeFulfillmentStatusAsync(
            order,
            FulfillmentStatuses.Preparing,
            actorId,
            reason ??
            "Seller started preparing the gemstone for dispatch.");

        return (

            await LoadOrderAsync(

                orderId)

        )!;

    }

    // =========================================================

    // READY FOR DISPATCH

    // =========================================================

    public async Task<OrderResponseDto>

        MarkReadyForDispatchAsync(

            Guid orderId,

            Guid actorId,

            string actorRole,

            string? reason)

    {

        var order =

            await _context.Orders

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        ValidateSellerOrAdmin(

            order,

            actorId,

            actorRole);

        if (

            order.Status !=

            OrderStatuses.Paid)

        {

            throw new InvalidOperationException(

                "Only paid orders can be dispatched.");

        }

        if (

            order.FulfillmentStatus !=

            FulfillmentStatuses.Preparing)

        {

            throw new InvalidOperationException(

                "The order must be in Preparing status before it can be marked ready for dispatch.");

        }

        await ChangeFulfillmentStatusAsync(
            order,
            FulfillmentStatuses.ReadyForDispatch,
            actorId,
            reason ??
            "Gemstone package is ready for courier dispatch.");

        return (

            await LoadOrderAsync(

                orderId)

        )!;

    }

    public async Task<OrderResponseDto>

        MarkInTransitAsync(

            Guid orderId,

            Guid actorId,

            string actorRole,

            string? reason)

    {

        var order =

            await _context.Orders

                .Include(o => o.Shipment)

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        ValidateSellerOrAdmin(

            order,

            actorId,

            actorRole);

        if (

            order.FulfillmentStatus !=

            FulfillmentStatuses

                .HandedOverToCourier)

        {

            throw new InvalidOperationException(

                "The order must first be handed over to the courier.");

        }

        if (order.Shipment == null)

        {

            throw new InvalidOperationException(

                "Shipment information is missing.");

        }

        order.Shipment.Status =

            FulfillmentStatuses.InTransit;

        order.Shipment.UpdatedAt =

            DateTime.UtcNow;

        await ChangeFulfillmentStatusAsync(
            order,
            FulfillmentStatuses.InTransit,
            actorId,
            reason ??
            "Shipment is now in transit.");

        return (

            await LoadOrderAsync(

                orderId)

        )!;

    }

    public async Task<OrderResponseDto>

        MarkOutForDeliveryAsync(

            Guid orderId,

            Guid actorId,

            string actorRole,

            string? reason)

    {

        var order =

            await _context.Orders

                .Include(o => o.Shipment)

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        ValidateSellerOrAdmin(

            order,

            actorId,

            actorRole);

        if (

            order.FulfillmentStatus !=

            FulfillmentStatuses

                .InTransit)

        {

            throw new InvalidOperationException(

                "The shipment must be in transit before it can be marked out for delivery.");

        }

        if (order.Shipment == null)

        {

            throw new InvalidOperationException(

                "Shipment information is missing.");

        }

        order.Shipment.Status =

            FulfillmentStatuses

                .OutForDelivery;

        order.Shipment.UpdatedAt =

            DateTime.UtcNow;

        await ChangeFulfillmentStatusAsync(
            order,
            FulfillmentStatuses.OutForDelivery,
            actorId,
            reason ??
            "Shipment is out for delivery.");

        return (

            await LoadOrderAsync(

                orderId)

        )!;

    }

    public async Task<OrderResponseDto>

        MarkDeliveredAsync(

            Guid orderId,

            Guid actorId,

            string actorRole,

            string? reason)

    {

        var order =

            await _context.Orders

                .Include(o => o.Shipment)

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        ValidateSellerOrAdmin(

            order,

            actorId,

            actorRole);

        if (

            order.FulfillmentStatus !=

            FulfillmentStatuses

                .OutForDelivery)

        {

            throw new InvalidOperationException(

                "The order must be out for delivery before being marked delivered.");

        }

        if (order.Shipment == null)

        {

            throw new InvalidOperationException(

                "Shipment information is missing.");

        }

        var now =

            DateTime.UtcNow;

        order.DeliveredAt =

            now;

        order.Shipment.Status =

            FulfillmentStatuses.Delivered;

        order.Shipment.DeliveredAt =

            now;

        order.Shipment.UpdatedAt =

            now;

        await ChangeFulfillmentStatusAsync(
            order,
            FulfillmentStatuses.Delivered,
            actorId,
            reason ??
            "Gemstone successfully delivered.");

        return (

            await LoadOrderAsync(

                orderId)

        )!;

    }

    public async Task<OrderResponseDto>

        CompleteAsync(

            Guid orderId,

            Guid buyerId,

            string? reason)

    {

        var order =

            await _context.Orders

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId &&

                    o.BuyerId == buyerId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        if (

            order.FulfillmentStatus !=

            FulfillmentStatuses.Delivered)

        {

            throw new InvalidOperationException(

                "The gemstone must be delivered before the order can be completed.");

        }

        if (

            order.Status !=

            OrderStatuses.Paid)

        {

            throw new InvalidOperationException(

                $"Order cannot be completed from status '{order.Status}'.");

        }

        await ChangeStatusAsync(

            order,

            OrderStatuses.Completed,

            buyerId,

            reason ??

            "Buyer confirmed successful delivery.");

        return (

            await LoadOrderAsync(

                orderId)

        )!;

    }

    // =========================================================

    // HAND OVER TO COURIER

    //

    // ADDRESS LOCKS HERE.

    // =========================================================

    public async Task<OrderResponseDto>

     HandOverToCourierAsync(

         Guid orderId,

         Guid actorId,

         string actorRole,

         CreateShipmentRequestDto dto)

    {

        var order =

            await _context.Orders

                .Include(o =>

                    o.DeliveryDetails)

                .Include(o =>

                    o.Shipment)

                .FirstOrDefaultAsync(o =>

                    o.Id == orderId)

            ?? throw new KeyNotFoundException(

                "Order was not found.");

        ValidateSellerOrAdmin(

            order,

            actorId,

            actorRole);

        if (

            order.Status !=

            OrderStatuses.Paid)

        {

            throw new InvalidOperationException(

                "The order must be paid before courier handover.");

        }

        if (

            order.FulfillmentStatus !=

            FulfillmentStatuses

                .ReadyForDispatch)

        {

            throw new InvalidOperationException(

                "The order must be ready for dispatch before courier handover.");

        }

        if (

            order.DeliveryDetails ==

            null)

        {

            throw new InvalidOperationException(

                "Delivery details are missing.");

        }

        ValidateStoredDeliveryDetails(
            order.DeliveryDetails);

        if (

            string.IsNullOrWhiteSpace(

                dto.CourierName))

        {

            throw new InvalidOperationException(

                "Courier name is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.TrackingNumber))

        {

            throw new InvalidOperationException(

                "Tracking number is required.");

        }

        if (order.Shipment != null)

        {

            throw new InvalidOperationException(

                "A shipment already exists for this order.");

        }

        var duplicateTracking =

            await _context.Shipments

                .AnyAsync(s =>

                    s.TrackingNumber ==

                    dto.TrackingNumber.Trim());

        if (duplicateTracking)

        {

            throw new InvalidOperationException(

                "This tracking number is already registered.");

        }

        var now =

            DateTime.UtcNow;

        var shipment =

            new Shipment

            {

                Id =

                    Guid.NewGuid(),

                OrderId =

                    order.Id,

                CourierName =

                    dto.CourierName.Trim(),

                TrackingNumber =

                    dto.TrackingNumber.Trim(),

                TrackingUrl =

                    string.IsNullOrWhiteSpace(

                        dto.TrackingUrl)

                        ? null

                        : dto.TrackingUrl.Trim(),

                ExpectedDeliveryDate =

                    dto.ExpectedDeliveryDate,

                DispatchNote =

                    string.IsNullOrWhiteSpace(

                        dto.DispatchNote)

                        ? null

                        : dto.DispatchNote.Trim(),

                Status =

                    FulfillmentStatuses

                        .HandedOverToCourier,

                CreatedAt =

                    now,

                UpdatedAt =

                    now,

                HandedOverAt =

                    now

            };

        _context.Shipments

            .Add(shipment);

        order.HandedOverAt =

            now;

        // Delivery information becomes immutable

        // after physical courier handover.

        order.DeliveryDetails.LockedAt =

            now;

        order.DeliveryDetails.UpdatedAt =

            now;

        await ChangeFulfillmentStatusAsync(
            order,
            FulfillmentStatuses.HandedOverToCourier,
            actorId,
            dto.DispatchNote ??
            $"Order handed over to {dto.CourierName.Trim()}.");

        return (

            await LoadOrderAsync(

                orderId)

        )!;

    }

    // =========================================================

    // CHANGE ORDER STATUS

    // =========================================================

    private async Task ChangeStatusAsync(

        Order order,

        string newStatus,

        Guid? actorId,

        string? reason)

    {

        var previousStatus =

            order.Status;

        order.Status =

            newStatus;

        order.UpdatedAt =

            DateTime.UtcNow;

        order.BuyerMessageAt = DateTime.UtcNow;

        var history =

            new OrderStatusHistory

            {

                OrderId =

                    order.Id,

                PreviousStatus =

                    previousStatus,

                NewStatus =

                    newStatus,

                ChangedByUserId =

                    actorId,

                Reason =

                    string.IsNullOrWhiteSpace(

                        reason)

                        ? null

                        : reason.Trim(),

                CreatedAt =

                    DateTime.UtcNow

            };

        _context

            .OrderStatusHistories

            .Add(history);

        await _context

            .SaveChangesAsync();

    }

    // =========================================================

    // QUERY

    // =========================================================

    private IQueryable<Order>

        LoadOrdersQuery()

    {

        return _context.Orders

            .AsNoTracking();

    }

    private async Task<OrderResponseDto?>

        LoadOrderAsync(

            Guid id,

            System.Linq.Expressions.Expression<

                Func<Order, bool>>?

                scope = null)

    {

        var query =

            LoadOrdersQuery()

                .Where(o =>

                    o.Id == id);

        if (scope != null)

        {

            query =

                query.Where(scope);

        }

        return await query

            .Select(

                OrderProjection())

            .FirstOrDefaultAsync();

    }

    // =========================================================

    // RESPONSE PROJECTION

    // =========================================================

    private static System.Linq.Expressions.Expression<

        Func<Order, OrderResponseDto>>

        OrderProjection()

    {

        return o =>

            new OrderResponseDto

            {

                Id =

                    o.Id,

                GemListingId =

                    o.GemListingId,

                GemTitle =

                    o.GemListing != null

                        ? o.GemListing.Title

                        : string.Empty,

                GemImageUrl =

                    o.GemListing != null

                        ? o.GemListing

                            .PrimaryImageUrl

                        : null,

                BuyerId =

                    o.BuyerId,

                BuyerName =

                    o.Buyer.FullName,

                SellerId =

                    o.SellerId,

                SellerName =

                    o.Seller.FullName,

                AgreedPrice =

                    o.TotalAmount,

                Currency =

                    o.Currency,

                Status =

                    o.Status,
                PaymentDueAt = o.PaymentDueAt,
                BuyerMessageAt = o.BuyerMessageAt,
                BuyerReadAt = o.BuyerReadAt,
                SellerReadAt = o.SellerReadAt,

                FulfillmentStatus =

                    o.FulfillmentStatus,

                HandedOverAt =

                    o.HandedOverAt,

                DeliveredAt =

                    o.DeliveredAt,

                ShippingAddress =

                    o.ShippingAddress,

                ShippingRegion =

                    o.ShippingRegion,

                ShippingCountryCode =

                    o.ShippingCountryCode,

                // =========================================

                // FULFILLMENT

                // =========================================

               FulfillmentHistory =

    o.FulfillmentStatusHistory

        .OrderBy(h =>

            h.CreatedAt)

        .Select(h =>

            new FulfillmentStatusHistoryDto

            {

                PreviousStatus =

                    h.PreviousStatus,

                NewStatus =

                    h.NewStatus,

                ChangedByName =

                    h.ChangedByUser != null

                        ? h.ChangedByUser.FullName

                        : "System",

                Note =

                    h.Note,

                CreatedAt =

                    h.CreatedAt

            })

        .ToList(),

                // =========================================

                // LEGACY SHIPPING FIELDS

                // =========================================

                Shipment =

    o.Shipment == null

        ? null

        : new ShipmentResponseDto

        {

            Id =

                o.Shipment.Id,

            CourierName =

                o.Shipment.CourierName,

            TrackingNumber =

                o.Shipment.TrackingNumber,

            TrackingUrl =

                o.Shipment.TrackingUrl,

            ExpectedDeliveryDate =

                o.Shipment.ExpectedDeliveryDate,

            DispatchNote =

                o.Shipment.DispatchNote,

            Status =

                o.Shipment.Status,

            HandedOverAt =

                o.Shipment.HandedOverAt,

            DeliveredAt =

                o.Shipment.DeliveredAt

        },

                // =========================================

                // STRUCTURED DELIVERY

                // =========================================

                DeliveryDetails =

                    o.DeliveryDetails == null

                        ? null

                        : new OrderDeliveryDetailsDto

                        {

                            RecipientName =

                                o.DeliveryDetails

                                    .RecipientName,

                            RecipientPhone =

                                o.DeliveryDetails

                                    .RecipientPhone,

                            AlternatePhone =

                                o.DeliveryDetails

                                    .AlternatePhone,

                            AddressLine1 =

                                o.DeliveryDetails

                                    .AddressLine1,

                            AddressLine2 =

                                o.DeliveryDetails

                                    .AddressLine2,

                            City =

                                o.DeliveryDetails

                                    .City,

                            District =

                                o.DeliveryDetails

                                    .District,

                            Region =

                                o.DeliveryDetails

                                    .Region,

                            PostalCode =

                                o.DeliveryDetails

                                    .PostalCode,

                            CountryCode =

                                o.DeliveryDetails

                                    .CountryCode,

                            NearestLandmark =

                                o.DeliveryDetails

                                    .NearestLandmark,

                            DeliveryInstructions =

                                o.DeliveryDetails

                                    .DeliveryInstructions,

                            SignatureRequired =

                                o.DeliveryDetails

                                    .SignatureRequired,

                            IsLocked =

                                o.DeliveryDetails

                                    .LockedAt != null,

                            LockedAt =

                                o.DeliveryDetails

                                    .LockedAt

                        },

                CreatedAt =

                    o.CreatedAt,

                UpdatedAt =

                    o.UpdatedAt,

                PaidAt =

                    o.PaidAt,

                StatusHistory =

                    o.StatusHistory

                        .OrderBy(h =>

                            h.CreatedAt)

                        .Select(h =>

                            new OrderStatusHistoryDto

                            {

                                PreviousStatus =

                                    h.PreviousStatus,

                                NewStatus =

                                    h.NewStatus,

                                ChangedByName =

                                    h.ChangedByUser !=

                                    null

                                        ? h.ChangedByUser

                                            .FullName

                                        : "System",

                                Reason =

                                    h.Reason,

                                CreatedAt =

                                    h.CreatedAt

                            })

                        .ToList()

            };

    }

    // =========================================================

    // DELIVERY HELPERS

    // =========================================================

    private static bool

        HasAnyStructuredDeliveryValue(

            DeliveryDetailsRequestDto? dto)

    {

        if (dto == null)

            return false;

        return

            !string.IsNullOrWhiteSpace(

                dto.RecipientName) ||

            !string.IsNullOrWhiteSpace(

                dto.RecipientPhone) ||

            !string.IsNullOrWhiteSpace(

                dto.AddressLine1) ||

            !string.IsNullOrWhiteSpace(

                dto.City) ||

            !string.IsNullOrWhiteSpace(

                dto.District) ||

            !string.IsNullOrWhiteSpace(

                dto.Region) ||

            !string.IsNullOrWhiteSpace(

                dto.PostalCode);

    }




private static OrderResponseDto
    MapOrderToDto(Order order)
{
    return new OrderResponseDto
    {
        Id =
            order.Id,

        GemListingId =
            order.GemListingId,

        GemTitle =
            order.GemListing?.Title
            ?? string.Empty,

        GemImageUrl =
            order.GemListing
                ?.PrimaryImageUrl,


        BuyerId =
            order.BuyerId,

        BuyerName =
            order.Buyer?.FullName
            ?? string.Empty,


        SellerId =
            order.SellerId,

        SellerName =
            order.Seller?.FullName
            ?? string.Empty,


        AgreedPrice =
            order.TotalAmount,

        Currency =
            order.Currency
            ?? "LKR",

        PaymentDueAt = order.PaymentDueAt,
        BuyerMessageAt = order.BuyerMessageAt,
        BuyerReadAt = order.BuyerReadAt,
        SellerReadAt = order.SellerReadAt,
        Status =
            order.Status
            ?? OrderStatuses.Pending,


        // =====================================================
        // FULFILLMENT
        // =====================================================

        FulfillmentStatus =
            string.IsNullOrWhiteSpace(
                order.FulfillmentStatus)
                ? FulfillmentStatuses.Pending
                : order.FulfillmentStatus,

        HandedOverAt =
            order.HandedOverAt,

        DeliveredAt =
            order.DeliveredAt,


        // =====================================================
        // LEGACY SHIPPING
        // =====================================================

        ShippingAddress =
            order.ShippingAddress
            ?? string.Empty,

        ShippingRegion =
            order.ShippingRegion
            ?? string.Empty,

        ShippingCountryCode =
            order.ShippingCountryCode
            ?? string.Empty,


        // =====================================================
        // AUDIT
        // =====================================================

        CreatedAt =
            order.CreatedAt,

        UpdatedAt =
            order.UpdatedAt,

        PaidAt =
            order.PaidAt,


        // =====================================================
        // STRUCTURED DELIVERY
        // =====================================================

        DeliveryDetails =
            order.DeliveryDetails == null
                ? null
                : new OrderDeliveryDetailsDto
                {
                    RecipientName =
                        order.DeliveryDetails
                            .RecipientName,

                    RecipientPhone =
                        order.DeliveryDetails
                            .RecipientPhone,

                    AlternatePhone =
                        order.DeliveryDetails
                            .AlternatePhone,

                    AddressLine1 =
                        order.DeliveryDetails
                            .AddressLine1,

                    AddressLine2 =
                        order.DeliveryDetails
                            .AddressLine2,

                    City =
                        order.DeliveryDetails
                            .City,

                    District =
                        order.DeliveryDetails
                            .District,

                    Region =
                        order.DeliveryDetails
                            .Region,

                    PostalCode =
                        order.DeliveryDetails
                            .PostalCode,

                    CountryCode =
                        order.DeliveryDetails
                            .CountryCode,

                    NearestLandmark =
                        order.DeliveryDetails
                            .NearestLandmark,

                    DeliveryInstructions =
                        order.DeliveryDetails
                            .DeliveryInstructions,

                    SignatureRequired =
                        order.DeliveryDetails
                            .SignatureRequired,

                    IsLocked =
                        order.DeliveryDetails
                            .LockedAt.HasValue,

                    LockedAt =
                        order.DeliveryDetails
                            .LockedAt
                },


        // =====================================================
        // SHIPMENT
        // =====================================================

        Shipment =
            order.Shipment == null
                ? null
                : new ShipmentResponseDto
                {
                    Id =
                        order.Shipment.Id,

                    CourierName =
                        order.Shipment
                            .CourierName,

                    TrackingNumber =
                        order.Shipment
                            .TrackingNumber,

                    TrackingUrl =
                        order.Shipment
                            .TrackingUrl,

                    ExpectedDeliveryDate =
                        order.Shipment
                            .ExpectedDeliveryDate,

                    DispatchNote =
                        order.Shipment
                            .DispatchNote,

                    Status =
                        order.Shipment
                            .Status,

                    HandedOverAt =
                        order.Shipment
                            .HandedOverAt,

                    DeliveredAt =
                        order.Shipment
                            .DeliveredAt
                },


        // =====================================================
        // TRANSACTION HISTORY
        // =====================================================

        StatusHistory =
            order.StatusHistory

                .OrderBy(h =>
                    h.CreatedAt)

                .Select(h =>
                    new OrderStatusHistoryDto
                    {
                        PreviousStatus =
                            h.PreviousStatus,

                        NewStatus =
                            h.NewStatus,

                        ChangedByName =
                            h.ChangedByUser
                                ?.FullName
                            ?? "System",

                        Reason =
                            h.Reason,

                        CreatedAt =
                            h.CreatedAt
                    })

                .ToList(),


        // =====================================================
        // FULFILLMENT HISTORY
        // =====================================================

        FulfillmentHistory =
            order.FulfillmentStatusHistory

                .OrderBy(h =>
                    h.CreatedAt)

                .Select(h =>
                    new FulfillmentStatusHistoryDto
                    {
                        PreviousStatus =
                            h.PreviousStatus,

                        NewStatus =
                            h.NewStatus,

                        ChangedByName =
                            h.ChangedByUser
                                ?.FullName
                            ?? "System",

                        Note =
                            h.Note,

                        CreatedAt =
                            h.CreatedAt
                    })

                .ToList()
    };
}





    private static void

        ValidateLegacyShipping(

            CreateOrderRequestDto dto)

    {

        if (

            string.IsNullOrWhiteSpace(

                dto.ShippingAddress) ||

            string.IsNullOrWhiteSpace(

                dto.ShippingRegion) ||

            string.IsNullOrWhiteSpace(

                dto.ShippingCountryCode))

        {

            throw new InvalidOperationException(

                "Delivery information is required.");

        }

    }

    private static void

        ValidateDeliveryDetails(

            DeliveryDetailsRequestDto dto)

    {

        if (

            string.IsNullOrWhiteSpace(

                dto.RecipientName))

        {

            throw new InvalidOperationException(

                "Recipient name is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.RecipientPhone))

        {

            throw new InvalidOperationException(

                "Recipient phone number is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.AddressLine1))

        {

            throw new InvalidOperationException(

                "Address line 1 is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.City))

        {

            throw new InvalidOperationException(

                "City or town is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.District))

        {

            throw new InvalidOperationException(

                "District is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.Region))

        {

            throw new InvalidOperationException(

                "Province or region is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.PostalCode))

        {

            throw new InvalidOperationException(

                "Postal code is required.");

        }

        if (

            string.IsNullOrWhiteSpace(

                dto.CountryCode) ||

            dto.CountryCode

                .Trim()

                .Length != 2)

        {

            throw new InvalidOperationException(

                "A valid two-letter country code is required.");

        }

    }

    private static void

        ValidateStoredDeliveryDetails(

            OrderDeliveryDetails details)

    {

        if (

            string.IsNullOrWhiteSpace(

                details.RecipientName) ||

            string.IsNullOrWhiteSpace(

                details.RecipientPhone) ||

            string.IsNullOrWhiteSpace(

                details.AddressLine1) ||

            string.IsNullOrWhiteSpace(

                details.City) ||

            string.IsNullOrWhiteSpace(

                details.District) ||

            string.IsNullOrWhiteSpace(

                details.Region) ||

            string.IsNullOrWhiteSpace(

                details.PostalCode) ||

            string.IsNullOrWhiteSpace(

                details.CountryCode))

        {

            throw new InvalidOperationException(

                "Delivery information is incomplete.");

        }

    }

    private static OrderDeliveryDetails

        CreateDeliveryEntity(

            Guid orderId,

            Guid buyerId,

            DeliveryDetailsRequestDto dto,

            DateTime now)

    {

        var entity =

            new OrderDeliveryDetails

            {

                Id =

                    Guid.NewGuid(),

                OrderId =

                    orderId,

                CreatedAt =

                    now

            };

        ApplyDeliveryDetails(

            entity,

            dto,

            buyerId,

            now);

        return entity;

    }

    private static void

        ApplyDeliveryDetails(

            OrderDeliveryDetails entity,

            DeliveryDetailsRequestDto dto,

            Guid buyerId,

            DateTime now)

    {

        entity.RecipientName =

            dto.RecipientName.Trim();

        entity.RecipientPhone =

            dto.RecipientPhone.Trim();

        entity.AlternatePhone =

            Clean(dto.AlternatePhone);

        entity.AddressLine1 =

            dto.AddressLine1.Trim();

        entity.AddressLine2 =

            Clean(dto.AddressLine2);

        entity.City =

            dto.City.Trim();

        entity.District =

            dto.District.Trim();

        entity.Region =

            dto.Region.Trim();

        entity.PostalCode =

            dto.PostalCode.Trim();

        entity.CountryCode =

            dto.CountryCode

                .Trim()

                .ToUpperInvariant();

        entity.NearestLandmark =

            Clean(

                dto.NearestLandmark);

        entity.DeliveryInstructions =

            Clean(

                dto.DeliveryInstructions);

        entity.SignatureRequired =

            true;

        entity.UpdatedAt =

            now;

        entity.LastUpdatedByUserId =

            buyerId;

    }

    private static string

        BuildLegacyAddress(

            DeliveryDetailsRequestDto dto)

    {

        var parts =

            new[]

            {

                dto.AddressLine1,

                dto.AddressLine2,

                dto.City,

                dto.District,

                dto.PostalCode

            }

            .Where(x =>

                !string.IsNullOrWhiteSpace(

                    x))

            .Select(x =>

                x!.Trim());

        return string.Join(

            ", ",

            parts);

    }

    private static string? Clean(

        string? value)

    {

        return string.IsNullOrWhiteSpace(

            value)

            ? null

            : value.Trim();

    }

    private static bool IsDeliveryLocked(

        Order order)

    {

        if (

            order.DeliveryDetails?.LockedAt !=

            null)

        {

            return true;

        }

        return

            order.FulfillmentStatus ==

                FulfillmentStatuses

                    .HandedOverToCourier ||

            order.FulfillmentStatus ==

                FulfillmentStatuses

                    .InTransit ||

            order.FulfillmentStatus ==

                FulfillmentStatuses

                    .OutForDelivery ||

            order.FulfillmentStatus ==

                FulfillmentStatuses

                    .Delivered ||

            order.FulfillmentStatus ==

                FulfillmentStatuses

                    .DeliveryFailed ||

            order.FulfillmentStatus ==

                FulfillmentStatuses

                    .Returned;

    }

    // =========================================================

    // AUTHORIZATION HELPER

    // =========================================================

    private static void

        ValidateSellerOrAdmin(

            Order order,

            Guid actorId,

            string actorRole)

    {

        if (

            actorRole !=

                UserRoles.Seller &&

            actorRole !=

                UserRoles.Admin)

        {

            throw new UnauthorizedAccessException(

                "Only the Seller or Admin can perform this action.");

        }

        if (

            actorRole ==

                UserRoles.Seller &&

            order.SellerId != actorId)

        {

            throw new UnauthorizedAccessException(

                "You can only manage orders for your own gemstone listings.");

        }

    }

}
