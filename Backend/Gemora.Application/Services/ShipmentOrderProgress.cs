using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

// Changes are staged in the caller's context so shipment, order and message save together.
public static class ShipmentOrderProgress
{
    public const string ApprovalMessage =
        "Your shipping plan has been approved. View My Orders for delivery progress.";

    public static async Task RecordApprovalAsync(
        ApplicationDbContext context, Shipment shipment, Guid? actorId)
    {
        var order = await context.Orders.FindAsync(shipment.OrderId);
        if (order == null || order.Status != OrderStatuses.Paid) return;

        var status = order.FulfillmentStatus == FulfillmentStatuses.Pending
            ? FulfillmentStatuses.Preparing
            : order.FulfillmentStatus;
        RecordUpdate(context, order, status, actorId, ApprovalMessage, DateTime.UtcNow);
    }

    public static async Task RecordShipmentStatusAsync(
        ApplicationDbContext context, Shipment shipment, Guid? actorId, bool reconcile = false)
    {
        var order = await context.Orders.FindAsync(shipment.OrderId);
        if (order == null || order.Status != OrderStatuses.Paid) return;

        var (status, message) = shipment.Status switch
        {
            "Booked" => (FulfillmentStatuses.ReadyForDispatch,
                "Courier booking has been recorded. Your gemstone is ready for dispatch."),
            "PickedUp" => (FulfillmentStatuses.HandedOverToCourier,
                "Your gemstone has been handed over to the courier."),
            "InTransit" => (FulfillmentStatuses.InTransit,
                "Your gemstone is in transit to your delivery address."),
            "OutForDelivery" => (FulfillmentStatuses.OutForDelivery,
                "Your gemstone is out for delivery."),
            "Delivered" => (FulfillmentStatuses.Delivered,
                "Your gemstone has been delivered. Confirm receipt in My Orders after checking your package to complete the order."),
            "DeliveryFailed" => (FulfillmentStatuses.DeliveryFailed,
                "Delivery was unsuccessful. The shipping team will arrange the next steps."),
            "CustomsHold" => (order.FulfillmentStatus,
                "Your shipment is awaiting customs clearance."),
            "Exception" => (order.FulfillmentStatus,
                "Your shipment needs attention. The shipping team is reviewing the delivery issue."),
            "Cancelled" => (FulfillmentStatuses.DeliveryFailed,
                "Your shipment was cancelled. The shipping team will contact you about the next steps."),
            _ => (null, null)
        };

        if (status == null || message == null) return;
        if (CourierBookingRules.IsSimulated(shipment))
            message = $"Academic simulation: {message}";
        if (reconcile && (status == order.FulfillmentStatus ||
            ProgressRank(status) < ProgressRank(order.FulfillmentStatus))) return;

        if (shipment.Status is "PickedUp" or "InTransit" or "OutForDelivery" or "Delivered")
        {
            order.HandedOverAt ??= shipment.HandedOverAt ?? shipment.ShippedAt ?? DateTime.UtcNow;
            var details = await context.OrderDeliveryDetails.FirstOrDefaultAsync(d => d.OrderId == order.Id);
            if (details != null) details.LockedAt ??= order.HandedOverAt;
        }
        if (shipment.Status == "Delivered")
            order.DeliveredAt = shipment.DeliveredAt ?? DateTime.UtcNow;

        RecordUpdate(context, order, status, actorId, message, DateTime.UtcNow);
    }

    // Restore messages missed by earlier versions using actual recorded approvals/delivery.
    // Run once on startup; history checks make repeated starts safe.
    public static async Task ReconcileExistingAsync(ApplicationDbContext context)
    {
        var shipments = await context.Shipments
            .Include(s => s.ShippingPlan)
            .Where(s => s.Order.Status == OrderStatuses.Paid &&
                s.ShippingPlan != null && s.ShippingPlan.IsApproved)
            .ToListAsync();

        foreach (var shipment in shipments)
        {
            var plan = shipment.ShippingPlan!;
            var approvedAt = plan.ApprovedAt ?? plan.CreatedAt;
            var alreadyNotified = await context.FulfillmentStatusHistories.AnyAsync(h =>
                h.OrderId == shipment.OrderId && h.Note == ApprovalMessage && h.CreatedAt >= approvedAt);
            if (!alreadyNotified)
                await RecordApprovalAsync(context, shipment, plan.ApprovedBy);
            await RecordShipmentStatusAsync(context, shipment, plan.ApprovedBy, reconcile: true);
        }

        await context.SaveChangesAsync();
    }

    private static int ProgressRank(string status) => status switch
    {
        FulfillmentStatuses.Pending => 0,
        FulfillmentStatuses.Preparing => 1,
        FulfillmentStatuses.ReadyForDispatch => 2,
        FulfillmentStatuses.HandedOverToCourier => 3,
        FulfillmentStatuses.InTransit => 4,
        FulfillmentStatuses.OutForDelivery => 5,
        FulfillmentStatuses.Delivered => 6,
        _ => 0
    };

    private static void RecordUpdate(ApplicationDbContext context, Order order,
        string status, Guid? actorId, string message, DateTime now)
    {
        context.FulfillmentStatusHistories.Add(new FulfillmentStatusHistory
        {
            OrderId = order.Id,
            PreviousStatus = order.FulfillmentStatus,
            NewStatus = status,
            ChangedByUserId = actorId,
            Note = message,
            CreatedAt = now
        });
        order.FulfillmentStatus = status;
        order.UpdatedAt = now;
        order.BuyerMessageAt = now;
    }
}
