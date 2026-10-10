using System.Data;
using Gemora.Application.DTOs.Orders;
using Gemora.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public partial class OrderService
{
    public async Task<OrderResponseDto> RejectAsync(Guid id, Guid sellerId, string? reason, bool alreadySold)
    {
        if (!alreadySold && (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000))
            throw new InvalidOperationException("Enter a rejection reason (1–1000 characters).");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await _context.Orders.Include(o => o.GemListing)
            .FirstOrDefaultAsync(o => o.Id == id && o.SellerId == sellerId)
            ?? throw new KeyNotFoundException("Order was not found.");
        if (order.Status != OrderStatuses.Pending)
            throw new InvalidOperationException("Only pending orders can be rejected.");

        if (alreadySold)
        {
            if (await _context.Orders.AnyAsync(o => o.GemListingId == order.GemListingId &&
                o.Status != OrderStatuses.Pending && o.Status != OrderStatuses.Rejected &&
                o.Status != OrderStatuses.Cancelled && o.Status != OrderStatuses.Refunded && o.Status != OrderStatuses.Failed))
                throw new InvalidOperationException("This gemstone already has an approved or paid order.");
            if (order.GemListing == null) throw new InvalidOperationException("Gem listing was not found.");
            order.GemListing.Status = GemListingStatuses.Sold;
            order.GemListing.UpdatedAt = DateTime.UtcNow;
            var pending = await _context.Orders.Where(o => o.GemListingId == order.GemListingId && o.Status == OrderStatuses.Pending).ToListAsync();
            foreach (var request in pending)
                await ChangeStatusAsync(request, OrderStatuses.Rejected, sellerId, "Already sold — the seller marked this gemstone as sold.");
        }
        else
            await ChangeStatusAsync(order, OrderStatuses.Rejected, sellerId, reason!.Trim());
        await transaction.CommitAsync();
        return (await LoadOrderAsync(id))!;
    }

    public async Task ExpireUnpaidAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var now = DateTime.UtcNow;
        var expired = await _context.Orders.Where(o =>
            (o.Status == OrderStatuses.Confirmed || o.Status == OrderStatuses.AwaitingPayment) &&
            o.PaymentDueAt != null && o.PaymentDueAt <= now && o.PaidAt == null).ToListAsync();
        foreach (var order in expired)
            await ChangeStatusAsync(order, OrderStatuses.Cancelled, null,
                "Payment deadline expired. Payment was not completed within 3 hours; the gemstone is available again.");
        await transaction.CommitAsync();
    }

    public async Task MarkMessageReadAsync(Guid id, Guid userId, bool seller, DateTime messageAt)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id &&
            (seller ? o.SellerId == userId : o.BuyerId == userId))
            ?? throw new KeyNotFoundException("Order was not found.");
        var latest = seller ? order.CreatedAt : order.BuyerMessageAt;
        if (latest == null || messageAt != latest) return; // A stale inbox must not mark a newer message as read.
        if (seller) order.SellerReadAt = latest;
        else order.BuyerReadAt = latest;
        await _context.SaveChangesAsync();
    }
}
