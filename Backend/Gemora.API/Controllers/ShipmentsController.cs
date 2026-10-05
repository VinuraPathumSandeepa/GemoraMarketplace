using Gemora.Application.DTOs;
using Gemora.Application.Services;
using Gemora.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly IShippingAgentService _shippingAgentService;
    private readonly ApplicationDbContext _context;

    public ShipmentsController(
        IShipmentService shipmentService,
        IShippingAgentService shippingAgentService,
        ApplicationDbContext context)
    {
        _shipmentService = shipmentService;
        _shippingAgentService = shippingAgentService;
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
        catch (UnauthorizedAccessException)
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
    /// Authorization: Seller (own), Buyer (own), Admin (all)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetShipmentById(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _shipmentService.GetShipmentByIdAsync(id);
            
            // Authorization check
            if (userRole != "Admin")
            {
                if (userRole == "Buyer" && result.BuyerId != userId)
                {
                    return Forbid();
                }

                if (userRole == "Seller" && result.SellerId != userId)
                {
                    return Forbid();
                }
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
    /// Authorization: Seller (own orders), Buyer (own orders), Admin (all)
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetShipmentsByOrderId(Guid orderId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // First verify the user has access to this order
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound(new { message = "Order not found." });
            }

            // Authorization check on order ownership
            if (userRole != "Admin")
            {
                if (userRole == "Buyer" && order.BuyerId != userId)
                {
                    return Forbid();
                }

                if (userRole == "Seller" && order.SellerId != userId)
                {
                    return Forbid();
                }
            }

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
        catch (UnauthorizedAccessException)
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
    /// Authorization: Seller (own shipments), Admin (all)
    /// </summary>
    [HttpPost("{id}/plan")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> GenerateShippingPlan(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Verify user has access to this shipment
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            // Authorization check
            if (userRole != "Admin")
            {
                if (userRole == "Seller" && shipment.SellerId != userId)
                {
                    return Forbid();
                }

                if (userRole == "Buyer")
                {
                    return Forbid("Buyers cannot generate shipping plans.");
                }
            }

            var result = await _shippingAgentService.GenerateShippingPlanAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get shipping plan
    /// Authorization: Seller (own), Buyer (own purchase), Admin (all)
    /// Returns persisted plan - does NOT regenerate
    /// </summary>
    [HttpGet("{id}/plan")]
    public async Task<IActionResult> GetShippingPlan(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Verify user has access to this shipment
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            // Authorization check
            if (userRole != "Admin")
            {
                if (userRole == "Buyer" && shipment.BuyerId != userId)
                {
                    return Forbid();
                }

                if (userRole == "Seller" && shipment.SellerId != userId)
                {
                    return Forbid();
                }
            }

            var plan = await _shippingAgentService.GetShippingPlanAsync(id);
            
            if (plan == null)
            {
                return NotFound(new { message = "No shipping plan has been generated for this shipment yet." });
            }

            return Ok(plan);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Approve shipping plan (Admin only)
    /// </summary>
    [HttpPost("{id}/plan/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveShippingPlan(Guid id, [FromBody] ApprovePlanRequest? request = null)
    {
        try
        {
            var adminId = GetCurrentUserId();
            var result = await _shippingAgentService.ApproveShippingPlanAsync(id, adminId, request?.Notes);
            return Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reject shipping plan (Admin only)
    /// Sets shipment status back to Pending so seller can create a new plan
    /// </summary>
    [HttpPost("{id}/plan/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectShippingPlan(Guid id, [FromBody] RejectPlanRequest? request = null)
    {
        try
        {
            var adminId = GetCurrentUserId();
            var result = await _shippingAgentService.RejectShippingPlanAsync(id, adminId, request?.Reason);
            return Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Request revision of shipping plan (Admin only)
    /// Sets shipment status to Planning so seller knows to regenerate the plan
    /// </summary>
    [HttpPost("{id}/plan/request-revision")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestRevisionShippingPlan(Guid id, [FromBody] RequestRevisionPlanRequest? request = null)
    {
        try
        {
            var adminId = GetCurrentUserId();
            var result = await _shippingAgentService.RequestRevisionShippingPlanAsync(id, adminId, request?.Notes);
            return Ok(new { success = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Book shipment with courier (Admin only)
    /// Uses simulated courier provider adapter - DEMO/SIM prefixed values
    /// </summary>
    [HttpPost("{id}/book")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BookShipment(Guid id)
    {
        try
        {
            var adminId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var result = await _shipmentService.BookShipmentAsync(id, adminId, userRole);
            
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                courierName = result.CourierName,
                externalReference = result.ExternalShipmentReference,
                trackingNumber = result.TrackingNumber,
                selectedService = result.SelectedService,
                message = "Shipment successfully booked with courier (SIMULATION)"
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get tracking events
    /// Authorization: Seller (own), Buyer (own), Admin (all)
    /// </summary>
    [HttpGet("{id}/tracking")]
    public async Task<IActionResult> GetTrackingEvents(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Verify user has access to this shipment
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            // Authorization check
            if (userRole != "Admin")
            {
                if (userRole == "Buyer" && shipment.BuyerId != userId)
                {
                    return Forbid();
                }

                if (userRole == "Seller" && shipment.SellerId != userId)
                {
                    return Forbid();
                }
            }

            var results = await _shipmentService.GetTrackingEventsAsync(id, userId, userRole);
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
        catch (UnauthorizedAccessException)
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
    /// Authorization: Seller (own), Buyer (own), Admin (all)
    /// </summary>
    [HttpGet("{id}/insurance")]
    public async Task<IActionResult> GetInsurance(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Verify user has access to this shipment
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            // Authorization check
            if (userRole != "Admin")
            {
                if (userRole == "Buyer" && shipment.BuyerId != userId)
                {
                    return Forbid();
                }

                if (userRole == "Seller" && shipment.SellerId != userId)
                {
                    return Forbid();
                }
            }

            var result = await _shipmentService.GetInsuranceRecordAsync(id, userId, userRole);
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
    /// Authorization: Admin (all), Seller (own shipments only)
    /// Buyers cannot create insurance
    /// </summary>
    [HttpPost("{id}/insurance")]
    public async Task<IActionResult> CreateInsurance(Guid id, [FromBody] CreateInsuranceRecordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Verify shipment exists
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            // Authorization check - buyers cannot create insurance
            if (userRole == "Buyer")
            {
                return Forbid("Buyers cannot create insurance records.");
            }

            // Sellers can only create insurance for their own shipments
            if (userRole == "Seller" && shipment.SellerId != userId)
            {
                return Forbid("You can only create insurance for your own shipments.");
            }

            // Ensure shipment ID matches
            request.ShipmentId = id;

            var result = await _shipmentService.CreateInsuranceRecordAsync(id, userId, userRole, request);
            return CreatedAtAction(nameof(GetInsurance), new { id }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get shipment audit/audit history (Admin only)
    /// Returns chronological operational history with actor information
    /// </summary>
    [HttpGet("{id}/audit")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetShipmentAuditHistory(Guid id)
    {
        try
        {
            // Verify shipment exists
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            // Get all tracking events (which now serve as audit trail) ordered chronologically
            var auditEvents = await _context.ShipmentTrackingEvents
                .Where(e => e.ShipmentId == id)
                .OrderByDescending(e => e.OccurredAt)
                .Select(e => new
                {
                    e.Id,
                    e.EventType,
                    e.Location,
                    e.Description,
                    e.PerformedByUserId,
                    e.PerformedByRole,
                    e.PreviousState,
                    e.NewState,
                    e.Reason,
                    e.OccurredAt,
                    e.RecordedAt
                })
                .ToListAsync();

            return Ok(auditEvents);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
