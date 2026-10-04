using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new InvalidOperationException("User ID not found in token.");
        }
        return Guid.Parse(userIdClaim);
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "Buyer";
    }

    /// <summary>
    /// Get orders eligible for shipment creation (Seller only)
    /// Returns paid orders belonging to the authenticated seller that don't have active shipments
    /// </summary>
    [HttpGet("my-shipment-eligible")]
    public async Task<IActionResult> GetShipmentEligibleOrders()
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userRole != "Seller")
            {
                return Forbid();
            }

            // Get paid orders for this seller that don't have active shipments
            var eligibleOrders = await _context.Orders
                .Where(o => o.SellerId == userId && o.Status == "Paid")
                .Where(o => !_context.Shipments.Any(s => s.OrderId == o.Id))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.GemListingId,
                    GemTitle = o.GemListing != null ? o.GemListing.Title : "Unknown Gemstone",
                    BuyerName = _context.Users.Where(u => u.Id == o.BuyerId).Select(u => u.FullName).FirstOrDefault(),
                    o.TotalAmount,
                    o.Currency,
                    o.Status,
                    o.ShippingAddress,
                    o.ShippingRegion,
                    o.ShippingCountryCode,
                    o.CreatedAt,
                    o.PaidAt
                })
                .ToListAsync();

            return Ok(eligibleOrders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching eligible orders.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get order by ID with authorization check
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var order = await _context.Orders
                .Where(o => o.Id == id)
                .Select(o => new
                {
                    o.Id,
                    o.BuyerId,
                    o.SellerId,
                    o.GemListingId,
                    o.TotalAmount,
                    o.Currency,
                    o.Status,
                    o.ShippingAddress,
                    o.ShippingRegion,
                    o.ShippingCountryCode,
                    o.CreatedAt,
                    o.UpdatedAt,
                    o.PaidAt
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            // Authorization: Buyer can see own orders, Seller can see own sales
            if (userRole == "Buyer" && order.BuyerId != userId)
            {
                return Forbid();
            }

            if (userRole == "Seller" && order.SellerId != userId)
            {
                return Forbid();
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching the order.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all orders for authenticated user (Buyer or Seller)
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            IQueryable<Order> query = _context.Orders;

            if (userRole == "Buyer")
            {
                query = query.Where(o => o.BuyerId == userId);
            }
            else if (userRole == "Seller")
            {
                query = query.Where(o => o.SellerId == userId);
            }
            else
            {
                return Forbid();
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.BuyerId,
                    o.SellerId,
                    o.TotalAmount,
                    o.Currency,
                    o.Status,
                    o.ShippingAddress,
                    o.ShippingRegion,
                    o.ShippingCountryCode,
                    o.CreatedAt,
                    o.PaidAt
                })
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching orders.", error = ex.Message });
        }
    }
}
