using Gemora.Application.DTOs;
using Gemora.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly IShippingAgentService _shippingAgentService;

    public ShipmentsController(
        IShipmentService shipmentService,
        IShippingAgentService shippingAgentService)
    {
        _shipmentService = shipmentService;
        _shippingAgentService = shippingAgentService;
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
    /// Create a new shipment (Seller only)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _shipmentService.CreateShipmentAsync(userId, userRole, request);
            return CreatedAtAction(nameof(GetShipmentById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get shipment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetShipmentById(Guid id)
    {
        try
        {
            var result = await _shipmentService.GetShipmentByIdAsync(id);
            
            // Authorization check
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            if (userRole == "Buyer" && result.BuyerId != userId)
            {
                return Forbid();
            }

            if (userRole == "Seller" && result.SellerId != userId)
            {
                return Forbid();
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get shipments by order ID
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetShipmentsByOrderId(Guid orderId)
    {
        try
        {
            var results = await _shipmentService.GetShipmentsByOrderIdAsync(orderId);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get authenticated user's shipments
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyShipments()
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var results = await _shipmentService.GetUserShipmentsAsync(userId, userRole);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update shipment status
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateShipmentStatus(Guid id, [FromBody] UpdateShipmentStatusDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _shipmentService.UpdateShipmentStatusAsync(id, userId, userRole, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generate shipping plan using AI
    /// </summary>
    [HttpPost("{id}/plan")]
    public async Task<IActionResult> GenerateShippingPlan(Guid id)
    {
        try
        {
            var result = await _shippingAgentService.GenerateShippingPlanAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get shipping plan
    /// </summary>
    [HttpGet("{id}/plan")]
    public async Task<IActionResult> GetShippingPlan(Guid id)
    {
        // Implementation would require adding a method to IShippingAgentService
        return StatusCode(501, new { message = "Get shipping plan endpoint not fully implemented yet." });
    }

    /// <summary>
    /// Approve shipping plan (Admin only)
    /// </summary>
    [HttpPost("{id}/plan/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveShippingPlan(Guid id)
    {
        try
        {
            var adminId = GetCurrentUserId();
            var result = await _shippingAgentService.ApproveShippingPlanAsync(id, adminId, null);
            return Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get tracking events
    /// </summary>
    [HttpGet("{id}/tracking")]
    public async Task<IActionResult> GetTrackingEvents(Guid id)
    {
        try
        {
            var results = await _shipmentService.GetTrackingEventsAsync(id);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add tracking event (Admin only)
    /// </summary>
    [HttpPost("{id}/tracking-events")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddTrackingEvent(Guid id, [FromBody] AddTrackingEventDto request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _shipmentService.AddTrackingEventAsync(id, userId, userRole, request);
            return CreatedAtAction(nameof(GetTrackingEvents), new { id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get insurance information
    /// </summary>
    [HttpGet("{id}/insurance")]
    public async Task<IActionResult> GetInsurance(Guid id)
    {
        try
        {
            var result = await _shipmentService.GetInsuranceRecordAsync(id);
            if (result == null)
            {
                return NotFound(new { message = "No insurance record found for this shipment." });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Create insurance record
    /// </summary>
    [HttpPost("{id}/insurance")]
    public async Task<IActionResult> CreateInsurance(Guid id, [FromBody] CreateInsuranceRecordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Ensure shipment ID matches
            request.ShipmentId = id;

            var result = await _shipmentService.CreateInsuranceRecordAsync(id, userId, userRole, request);
            return CreatedAtAction(nameof(GetInsurance), new { id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
