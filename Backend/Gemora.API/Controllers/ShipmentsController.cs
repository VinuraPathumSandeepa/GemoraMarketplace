using System.Security.Claims;
using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateShipment([FromBody] CreateShipmentRequestDto dto)
    {
        var buyerId = GetCurrentUserId();
        var sellerId = GetCurrentUserId();

        if (!User.IsInRole(UserRoles.Seller) && !User.IsInRole(UserRoles.Admin))
        {
            return Forbid();
        }

        try
        {
            var shipment = await _shipmentService.CreateShipmentAsync(dto, buyerId, sellerId);
            return CreatedAtAction(nameof(GetShipmentById), new { id = shipment.Id }, shipment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetShipmentById(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        try
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id, currentUserId, role);
            if (shipment == null)
            {
                return NotFound();
            }

            return Ok(shipment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyShipments()
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        var shipments = await _shipmentService.GetMyShipmentsAsync(currentUserId, role);
        return Ok(shipments);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateShipmentStatus(Guid id, [FromBody] UpdateShipmentStatusRequestDto dto)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        try
        {
            var shipment = await _shipmentService.UpdateShipmentStatusAsync(id, dto, currentUserId, role);
            return Ok(shipment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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

    [HttpPost("{id:guid}/insurance")]
    public async Task<IActionResult> CreateInsuranceRecord(Guid id, [FromBody] CreateInsuranceRecordRequestDto dto)
    {
        if (dto.ShipmentId != id)
        {
            dto.ShipmentId = id;
        }

        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        try
        {
            var insurance = await _shipmentService.CreateInsuranceRecordAsync(dto, currentUserId, role);
            return Ok(insurance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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

    [HttpGet("{id:guid}/insurance")]
    public async Task<IActionResult> GetShipmentInsurance(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        try
        {
            var insurance = await _shipmentService.GetInsuranceRecordsAsync(id, currentUserId, role);
            return Ok(insurance);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}/tracking")]
    public async Task<IActionResult> GetTracking(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        try
        {
            var tracking = await _shipmentService.GetTrackingEventsAsync(id, currentUserId, role);
            return Ok(tracking);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:guid}/plan")]
    public async Task<IActionResult> GenerateShippingPlan(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        var role = GetCurrentUserRole();

        try
        {
            var plan = await _shipmentService.CreateShippingPlanAsync(id, currentUserId, role);
            return Ok(plan);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }
}
