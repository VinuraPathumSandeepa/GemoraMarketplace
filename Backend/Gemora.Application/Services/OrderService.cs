using System.Data;
using Gemora.Application.DTOs.Orders;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

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
            var listing = await _context.GemListings
                .FirstOrDefaultAsync(g =>
                    g.Id == dto.GemListingId);

            if (listing == null)
                throw new KeyNotFoundException(
                    "Gem listing was not found.");

            if (listing.Status != GemListingStatuses.Approved)
                throw new InvalidOperationException(
                    "Only approved gemstones can be purchased.");

            if (listing.SellerId == buyerId)
                throw new InvalidOperationException(
                    "You cannot purchase your own listing.");

            // No IsAvailable DB column.
            // Availability is determined using active orders.
            var hasActiveOrder = await _context.Orders
                .AnyAsync(o =>
                    o.GemListingId == listing.Id &&
                    o.Status != OrderStatuses.Cancelled &&
                    o.Status != OrderStatuses.Refunded &&
                    o.Status != OrderStatuses.Failed);

            if (hasActiveOrder)
                throw new InvalidOperationException(
                    "This gemstone is not currently available for purchase.");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = listing.SellerId,
                GemListingId = listing.Id,

                // DB uses TotalAmount
                TotalAmount = listing.Price,

                Currency = listing.Currency,
                Status = OrderStatuses.Pending,

                ShippingAddress =
                    dto.ShippingAddress.Trim(),

                ShippingRegion =
                    dto.ShippingRegion.Trim(),

                ShippingCountryCode =
                    dto.ShippingCountryCode
                        .Trim()
                        .ToUpperInvariant(),

                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            await _context.SaveChangesAsync();

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                PreviousStatus = null,
                NewStatus = OrderStatuses.Pending,
                ChangedByUserId = buyerId,
                Reason = "Order created by Buyer.",
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return (await LoadOrderAsync(order.Id))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Task<OrderResponseDto?> GetForBuyerAsync(
        Guid id,
        Guid buyerId)
    {
        return LoadOrderAsync(
            id,
            o => o.BuyerId == buyerId);
    }

    public async Task<List<OrderResponseDto>>
        GetMyOrdersAsync(Guid buyerId)
    {
        return await LoadOrdersQuery()
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(OrderProjection())
            .ToListAsync();
    }

    public Task<OrderResponseDto?> GetForSellerAsync(
        Guid id,
        Guid sellerId)
    {
        return LoadOrderAsync(
            id,
            o => o.SellerId == sellerId);
    }

    public async Task<List<OrderResponseDto>>
        GetSellerOrdersAsync(Guid sellerId)
    {
        return await LoadOrdersQuery()
            .Where(o => o.SellerId == sellerId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(OrderProjection())
            .ToListAsync();
    }

    public async Task<List<OrderResponseDto>>
        GetAllForAdminAsync()
    {
        return await LoadOrdersQuery()
            .OrderByDescending(o => o.CreatedAt)
            .Select(OrderProjection())
            .ToListAsync();
    }

    public async Task<OrderResponseDto> CancelAsync(
        Guid id,
        Guid buyerId,
        string? reason)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.Id == id &&
                o.BuyerId == buyerId)
            ?? throw new KeyNotFoundException(
                "Order was not found.");

        if (order.Status != OrderStatuses.Pending &&
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
            reason ?? "Cancelled by Buyer.");

        return (await LoadOrderAsync(id))!;
    }

    public async Task<OrderResponseDto> ConfirmAsync(
        Guid id,
        Guid actorId,
        string actorRole,
        string? reason)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new KeyNotFoundException(
                "Order was not found.");

        if (actorRole == UserRoles.Seller &&
            order.SellerId != actorId)
        {
            throw new UnauthorizedAccessException(
                "You can only confirm orders for your own listings.");
        }

        if (actorRole != UserRoles.Seller &&
            actorRole != UserRoles.Admin)
        {
            throw new UnauthorizedAccessException(
                "Only the Seller or Admin can confirm this order.");
        }

        if (order.Status != OrderStatuses.Pending)
        {
            throw new InvalidOperationException(
                $"Order cannot be confirmed from status '{order.Status}'.");
        }

        await ChangeStatusAsync(
            order,
            OrderStatuses.Confirmed,
            actorId,
            reason ?? "Order confirmed.");

        return (await LoadOrderAsync(id))!;
    }



    public async Task<PaymentResponseDto> PayAsync(
        Guid orderId,
        Guid buyerId,
        CreatePaymentRequestDto dto)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable);

        try
        {
            var order =
                await _context.Orders
                    .FirstOrDefaultAsync(o =>
                        o.Id == orderId &&
                        o.BuyerId == buyerId)
                ?? throw new KeyNotFoundException(
                    "Order was not found.");

            // =========================================
            // PAYMENT IS ONLY ALLOWED AFTER
            // SELLER CONFIRMATION
            // =========================================

            if (
                order.Status !=
                    OrderStatuses.Confirmed &&

                order.Status !=
                    OrderStatuses.AwaitingPayment)
            {
                throw new InvalidOperationException(
                    $"Payment cannot be completed from status '{order.Status}'.");
            }


            // =========================================
            // PREVENT DUPLICATE PAYMENT
            // =========================================

            var successfulPaymentExists =
                await _context.PaymentTransactions
                    .AnyAsync(p =>
                        p.OrderId == order.Id &&
                        p.Status ==
                            PaymentStatuses.Succeeded);

            if (
                successfulPaymentExists ||
                order.PaidAt.HasValue ||
                order.Status == OrderStatuses.Paid)
            {
                throw new InvalidOperationException(
                    "This order has already been paid.");
            }


            // =========================================
            // CREATE PAYMENT TRANSACTION
            // =========================================

            var now =
                DateTime.UtcNow;

            var externalReference =
                $"GEM-PAY-{Guid.NewGuid():N}"
                    .ToUpperInvariant();

            var payment =
                new PaymentTransaction
                {
                    Id =
                        Guid.NewGuid(),

                    OrderId =
                        order.Id,

                    // Demo/internal provider for now.
                    // Later replace with PayHere/
                    // Stripe/etc.
                    Provider =
                        "GemoraDemo",

                    ExternalReference =
                        externalReference,

                    Amount =
                        order.TotalAmount,

                    Currency =
                        order.Currency,

                    Status =
                        PaymentStatuses.Succeeded,

                    CreatedAt =
                        now,

                    UpdatedAt =
                        now
                };

            _context.PaymentTransactions
                .Add(payment);


            // =========================================
            // UPDATE ORDER
            // =========================================

            order.PaidAt =
                now;


            // This method already:
            // - changes Order.Status
            // - updates UpdatedAt
            // - adds OrderStatusHistory
            // - calls SaveChangesAsync()
            await ChangeStatusAsync(
                order,
                OrderStatuses.Paid,
                buyerId,
                string.IsNullOrWhiteSpace(
                    dto.PaymentMethod)
                    ? "Payment completed by Buyer."
                    : $"Payment completed by Buyer using {dto.PaymentMethod.Trim()}.");


            await transaction
                .CommitAsync();


            var updatedOrder =
                await LoadOrderAsync(
                    order.Id)
                ?? throw new InvalidOperationException(
                    "Unable to reload the paid order.");


            return new PaymentResponseDto
            {
                Id =
                    payment.Id,

                OrderId =
                    payment.OrderId,

                Provider =
                    payment.Provider,

                ExternalReference =
                    payment.ExternalReference,

                Amount =
                    payment.Amount,

                Currency =
                    payment.Currency,

                Status =
                    payment.Status,

                CreatedAt =
                    payment.CreatedAt,

                Order =
                    updatedOrder
            };
        }
        catch
        {
            await transaction
                .RollbackAsync();

            throw;
        }
    }







    private async Task ChangeStatusAsync(
        Order order,
        string newStatus,
        Guid actorId,
        string? reason)
    {
        var previousStatus = order.Status;

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        var history = new OrderStatusHistory
        {
            OrderId = order.Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = actorId,
            Reason = string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.OrderStatusHistories.Add(history);

        await _context.SaveChangesAsync();
    }

    private IQueryable<Order> LoadOrdersQuery()
    {
        return _context.Orders
            .AsNoTracking();
    }

    private async Task<OrderResponseDto?> LoadOrderAsync(
        Guid id,
        System.Linq.Expressions.Expression<Func<Order, bool>>?
            scope = null)
    {
        var query = LoadOrdersQuery()
            .Where(o => o.Id == id);

        if (scope != null)
            query = query.Where(scope);

        return await query
            .Select(OrderProjection())
            .FirstOrDefaultAsync();
    }

    private static System.Linq.Expressions.Expression<
        Func<Order, OrderResponseDto>> OrderProjection()
    {
        return o => new OrderResponseDto
        {
            Id = o.Id,

            GemListingId = o.GemListingId,

            GemTitle = o.GemListing != null
                ? o.GemListing.Title
                : string.Empty,

            GemImageUrl = o.GemListing != null
                ? o.GemListing.PrimaryImageUrl
                : null,

            BuyerId = o.BuyerId,
            BuyerName = o.Buyer.FullName,

            SellerId = o.SellerId,
            SellerName = o.Seller.FullName,

            // API can still call this AgreedPrice,
            // DB field is TotalAmount.
            AgreedPrice = o.TotalAmount,

            Currency = o.Currency,
            Status = o.Status,

            ShippingAddress = o.ShippingAddress,
            ShippingRegion = o.ShippingRegion,
            ShippingCountryCode = o.ShippingCountryCode,

            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            PaidAt = o.PaidAt,

            StatusHistory = o.StatusHistory
                .OrderBy(h => h.CreatedAt)
                .Select(h => new OrderStatusHistoryDto
                {
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,

                    ChangedByName =
                        h.ChangedByUser != null
                            ? h.ChangedByUser.FullName
                            : "System",

                    Reason = h.Reason,
                    CreatedAt = h.CreatedAt
                })
                .ToList()
        };
    }
}