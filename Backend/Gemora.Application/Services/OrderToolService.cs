using Gemora.Application.Interfaces;
using Gemora.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gemora.Application.Services;

/// <summary>
/// Implementation of IOrderToolService that retrieves order information from trusted backend sources.
/// This is an allow-listed tool function used by the Logistics & Courier Tool Agent.
/// </summary>
public class OrderToolService : IOrderToolService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderToolService> _logger;

    public OrderToolService(
        IOrderRepository orderRepository,
        ILogger<OrderToolService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<OrderInfo?> ReadOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool: Reading order information for order {OrderId}", orderId);

        var order = await _orderRepository.GetByIdWithUsersAsync(orderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            return null;
        }

        // Map to OrderInfo DTO with sanitized data
        return new OrderInfo
        {
            Id = order.Id,
            BuyerUserId = order.BuyerUserId,
            SellerUserId = order.SellerUserId,
            DeclaredValue = order.TotalAmount,
            Currency = order.Currency,
            Status = order.Status.ToString(),
            Origin = "Sri Lanka", // Default origin - should come from seller profile in production
            Destination = "Domestic", // Default destination - should come from buyer address in production
            RequiresExport = false, // Simplified for demo - would check if origin != destination country
            WeightInCarats = null // Not tracked in basic Order entity - would come from gem details
        };
    }
}
