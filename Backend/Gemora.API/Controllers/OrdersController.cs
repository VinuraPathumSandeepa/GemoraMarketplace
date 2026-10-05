using System.Security.Claims;
using Gemora.Application.DTOs.Orders;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    public OrdersController(
        IOrderService service)
    {
        _service = service;
    }


    // =========================================================
    // CREATE ORDER
    // =========================================================

    [HttpPost]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<
        ActionResult<OrderResponseDto>>
        Create(
            CreateOrderRequestDto dto)
    {
        var order =
            await _service.CreateAsync(
                CurrentUserId(),
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = order.Id
            },
            order);
    }


    // =========================================================
    // BUYER ORDERS
    // =========================================================

    [HttpGet("my")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<
        ActionResult<List<OrderResponseDto>>>
        GetMyOrders()
    {
        return Ok(
            await _service
                .GetMyOrdersAsync(
                    CurrentUserId()));
    }


    // =========================================================
    // SINGLE BUYER ORDER
    // =========================================================

    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<IActionResult>
        GetById(Guid id)
    {
        var order =
            await _service
                .GetForBuyerAsync(
                    id,
                    CurrentUserId());

        return order == null
            ? NotFound(
                new
                {
                    message =
                        "Order was not found."
                })
            : Ok(order);
    }


    // =========================================================
    // CANCEL ORDER
    // =========================================================

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<IActionResult>
        Cancel(
            Guid id,
            OrderActionRequestDto request)
    {
        return Ok(
            await _service.CancelAsync(
                id,
                CurrentUserId(),
                request.Reason));
    }


    // =========================================================
    // SELLER / ADMIN CONFIRM ORDER
    // =========================================================

    [HttpPost("{id:guid}/confirm")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<IActionResult>
        Confirm(
            Guid id,
            OrderActionRequestDto request)
    {
        return Ok(
            await _service
                .ConfirmAsync(
                    id,
                    CurrentUserId(),

                    User.FindFirstValue(
                        ClaimTypes.Role)
                    ?? "",

                    request.Reason));
    }


    // =========================================================
    // BUYER PAYMENT
    // =========================================================

    [HttpPost("{id:guid}/payment")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<
        ActionResult<PaymentResponseDto>>
        Pay(
            Guid id,
            CreatePaymentRequestDto request)
    {
        var payment =
            await _service.PayAsync(
                id,
                CurrentUserId(),
                request);

        return Ok(payment);
    }


    // =========================================================
    // CURRENT USER
    // =========================================================

    private Guid CurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            value,
            out var id)
            ? id
            : throw new
                UnauthorizedAccessException(
                    "Invalid authenticated user.");
    }
}