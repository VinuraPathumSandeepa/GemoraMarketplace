using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gemora.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ShipmentController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly ILogger<ShipmentController> _logger;

    public ShipmentController(
        IShipmentService shipmentService,
        ILogger<ShipmentController> logger)
    {
        _shipmentService = shipmentService;
        _logger = logger;
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

    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value 
               ?? User.FindFirst("role")?.Value 
               ?? throw new InvalidOperationException("User role not found in authentication token");
    }

    /// <summary>
    /// Creates a new shipment for an eligible order (Seller only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentDto request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var result = await _shipmentService.CreateShipmentAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetShipmentById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create shipment for order {OrderId}", request.OrderId);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating shipment");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Gets shipment details by ID with authorization checks.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetShipmentById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var userRole = GetUserRole();
            var result = await _shipmentService.GetShipmentByIdAsync(id, userId, userRole, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Shipment not found or access denied" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Gets shipment by order ID.
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetShipmentByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var userRole = GetUserRole();
            var result = await _shipmentService.GetShipmentByOrderIdAsync(orderId, userId, userRole, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Shipment not found for this order or access denied" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipment for order {OrderId}", orderId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Gets all shipments for the authenticated user based on their role.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyShipments(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var userRole = GetUserRole();
            var result = await _shipmentService.GetUserShipmentsAsync(userId, userRole, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user shipments");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Updates shipment status with validation (restricted by role).
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateShipmentStatus(
        Guid id,
        [FromBody] UpdateShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var userRole = GetUserRole();
            var result = await _shipmentService.UpdateShipmentStatusAsync(
                id, request.NewStatus, userId, userRole, request.Reason, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update status for shipment {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shipment status {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Generates a shipping plan using the four-agent AI subsystem.
    /// </summary>
    [HttpPost("{id:guid}/plan")]
    public async Task<IActionResult> GenerateShippingPlan(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _shipmentService.GenerateShippingPlanAsync(id, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    error = result.ErrorMessage,
                    validationErrors = result.ValidationErrors
                });
            }

            return Ok(result.Plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating shipping plan for shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Gets the shipping plan for a shipment.
    /// </summary>
    [HttpGet("{id:guid}/plan")]
    public async Task<IActionResult> GetShippingPlan(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _shipmentService.GetShippingPlanAsync(id, cancellationToken);

            if (result == null)
            {
                return NotFound(new { error = "Shipping plan not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipping plan for shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Approves a shipping plan (Admin only).
    /// </summary>
    [HttpPost("{id:guid}/plan/approve")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> ApproveShippingPlan(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var adminUserId = GetAuthenticatedUserId();
            var result = await _shipmentService.ApproveShippingPlanAsync(id, adminUserId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to approve shipping plan for shipment {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving shipping plan for shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Gets tracking events for a shipment.
    /// </summary>
    [HttpGet("{id:guid}/tracking")]
    public async Task<IActionResult> GetTrackingEvents(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var userRole = GetUserRole();
            var result = await _shipmentService.GetShipmentTrackingEventsAsync(id, userId, userRole, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tracking events for shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Gets insurance information for a shipment.
    /// </summary>
    [HttpGet("{id:guid}/insurance")]
    public async Task<IActionResult> GetInsurance(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            var userRole = GetUserRole();
            var result = await _shipmentService.GetShipmentInsuranceRecordsAsync(id, userId, userRole, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving insurance for shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Adds a tracking event to a shipment (Admin only).
    /// </summary>
    [HttpPost("{id:guid}/tracking-events")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> AddTrackingEvent(
        Guid id,
        [FromBody] CreateTrackingEventRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminUserId = GetAuthenticatedUserId();
            var result = await _shipmentService.AddTrackingEventAsync(id, request, adminUserId, cancellationToken);
            return CreatedAtAction(nameof(GetShipmentById), new { id = result.ShipmentId }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to add tracking event for shipment {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding tracking event for shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Creates an insurance record for a shipment (Admin only).
    /// </summary>
    [HttpPost("{id:guid}/insurance")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> CreateInsurance(
        Guid id,
        [FromBody] CreateInsuranceRecordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var adminUserId = GetAuthenticatedUserId();
            var result = await _shipmentService.CreateInsuranceRecordAsync(id, request, adminUserId, cancellationToken);
            return CreatedAtAction(nameof(GetInsurance), new { id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create insurance for shipment {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating insurance for shipment {Id}", id);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}
