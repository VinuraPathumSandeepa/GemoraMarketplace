using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gemora.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;

    public OrderController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in authentication token");
        }

        return userId;
    }

    /// <summary>
    /// Lists all orders for the authenticated user (as buyer or seller).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var orders = await _orderRepository.GetByUserIdAsync(userId, cancellationToken);
            
            var orderDtos = orders.Select(o => new
            {
                o.Id,
                o.BuyerUserId,
                o.SellerUserId,
                o.TotalAmount,
                o.Currency,
                o.Status,
                o.CreatedAt
            }).ToList();

            return Ok(orderDtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }
}
