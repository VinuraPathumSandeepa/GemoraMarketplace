using System.Security.Claims;
using Gemora.Application.DTOs.Orders;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/seller/orders")]
[Authorize(Roles = UserRoles.Seller)]
public class SellerOrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public SellerOrdersController(IOrderService service) => _service = service;

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
        => Ok(await _service.ConfirmAsync(id, CurrentUserId(), UserRoles.Seller, null));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, OrderDecisionDto request)
        => Ok(await _service.RejectAsync(id, CurrentUserId(), request.Reason, request.AlreadySold));

    [HttpPost("{id:guid}/message/read")]
    public async Task<IActionResult> ReadMessage(Guid id, ReadOrderMessageDto request)
    {
        await _service.MarkMessageReadAsync(id, CurrentUserId(), true, request.MessageAt);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAll()
        => Ok(await _service.GetSellerOrdersAsync(CurrentUserId()));



    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order =
            await _service.GetForSellerAsync(
                id,
                CurrentUserId());

        return order == null
            ? NotFound(new { message = "Order was not found." })
            : Ok(order);
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("Invalid authenticated user.");
    }
}
