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
    // BUYER - CREATE ORDER
    // =========================================================

    [HttpPost]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<ActionResult<OrderResponseDto>>
        Create(CreateOrderRequestDto dto)
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
    // BUYER - MY ORDERS
    // =========================================================

    [HttpGet("my")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<ActionResult<List<OrderResponseDto>>>
        GetMyOrders()
    {
        var orders =
            await _service.GetMyOrdersAsync(
                CurrentUserId());

        return Ok(orders);
    }


    // =========================================================
    // BUYER - SINGLE ORDER
    // =========================================================

    [HttpGet("{id:guid}")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<ActionResult<OrderResponseDto>>
        GetById(Guid id)
    {
        var order =
            await _service.GetForBuyerAsync(
                id,
                CurrentUserId());

        if (order == null)
        {
            return NotFound(
                new
                {
                    message =
                        "Order was not found."
                });
        }

        return Ok(order);
    }


    // =========================================================
    // BUYER - CANCEL ORDER
    // =========================================================

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<ActionResult<OrderResponseDto>>
        Cancel(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service.CancelAsync(
                id,
                CurrentUserId(),
                request.Reason);

        return Ok(order);
    }


    // =========================================================
    // SELLER / ADMIN - CONFIRM ORDER
    // =========================================================

    [HttpPost("{id:guid}/confirm")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<ActionResult<OrderResponseDto>>
        Confirm(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service.ConfirmAsync(
                id,
                CurrentUserId(),
                CurrentUserRole(),
                request.Reason);

        return Ok(order);
    }


    // =========================================================
    // BUYER - UPDATE DELIVERY DETAILS
    //
    // Editable until courier handover.
    // Backend service enforces the lock.
    // =========================================================

    [HttpPatch("{id:guid}/delivery-details")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<ActionResult<OrderResponseDto>>
        UpdateDeliveryDetails(
            Guid id,
            DeliveryDetailsRequestDto request)
    {
        var order =
            await _service
                .UpdateDeliveryDetailsAsync(
                    id,
                    CurrentUserId(),
                    request);

        return Ok(order);
    }


    // =========================================================
    // BUYER - PAYMENT
    // =========================================================

    [HttpPost("{id:guid}/payment")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<ActionResult<PaymentResponseDto>>
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
    // SELLER / ADMIN - START PREPARING
    //
    // Paid -> Preparing
    // =========================================================

    [HttpPost("{id:guid}/prepare")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<ActionResult<OrderResponseDto>>
        Prepare(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service
                .StartPreparingAsync(
                    id,
                    CurrentUserId(),
                    CurrentUserRole(),
                    request.Reason);

        return Ok(order);
    }


    // =========================================================
    // SELLER / ADMIN - READY FOR DISPATCH
    //
    // Preparing -> ReadyForDispatch
    // =========================================================

    [HttpPost("{id:guid}/ready-for-dispatch")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<ActionResult<OrderResponseDto>>
        ReadyForDispatch(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service
                .MarkReadyForDispatchAsync(
                    id,
                    CurrentUserId(),
                    CurrentUserRole(),
                    request.Reason);

        return Ok(order);
    }


    // =========================================================
    // SELLER / ADMIN - COURIER HANDOVER
    //
    // ReadyForDispatch -> HandedOverToCourier
    //
    // Creates Shipment:
    // - CourierName
    // - TrackingNumber
    // - TrackingUrl
    // - ExpectedDeliveryDate
    // - DispatchNote
    //
    // Delivery address becomes locked here.
    // =========================================================

    [HttpPost("{id:guid}/handover")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<ActionResult<OrderResponseDto>>
        HandOver(
            Guid id,
            CreateShipmentRequestDto request)
    {
        var order =
            await _service
                .HandOverToCourierAsync(
                    id,
                    CurrentUserId(),
                    CurrentUserRole(),
                    request);

        return Ok(order);
    }


    // =========================================================
    // SELLER / ADMIN - IN TRANSIT
    //
    // HandedOverToCourier -> InTransit
    // =========================================================

    [HttpPost("{id:guid}/in-transit")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<ActionResult<OrderResponseDto>>
        InTransit(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service
                .MarkInTransitAsync(
                    id,
                    CurrentUserId(),
                    CurrentUserRole(),
                    request.Reason);

        return Ok(order);
    }


    // =========================================================
    // SELLER / ADMIN - OUT FOR DELIVERY
    //
    // InTransit -> OutForDelivery
    // =========================================================

    [HttpPost("{id:guid}/out-for-delivery")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<ActionResult<OrderResponseDto>>
        OutForDelivery(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service
                .MarkOutForDeliveryAsync(
                    id,
                    CurrentUserId(),
                    CurrentUserRole(),
                    request.Reason);

        return Ok(order);
    }


    // =========================================================
    // SELLER / ADMIN - DELIVERED
    //
    // OutForDelivery -> Delivered
    // =========================================================

    [HttpPost("{id:guid}/delivered")]
    [Authorize(
        Roles =
            $"{UserRoles.Seller},{UserRoles.Admin}")]
    public async Task<ActionResult<OrderResponseDto>>
        Delivered(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service
                .MarkDeliveredAsync(
                    id,
                    CurrentUserId(),
                    CurrentUserRole(),
                    request.Reason);

        return Ok(order);
    }


    // =========================================================
    // BUYER - COMPLETE ORDER
    //
    // Delivered -> Completed
    //
    // Buyer confirms successful receipt.
    // =========================================================

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<ActionResult<OrderResponseDto>>
        Complete(
            Guid id,
            OrderActionRequestDto request)
    {
        var order =
            await _service.CompleteAsync(
                id,
                CurrentUserId(),
                request.Reason);

        return Ok(order);
    }


    // =========================================================
    // AUTHENTICATED USER ID
    // =========================================================

    [HttpPost("{id:guid}/message/read")]
    [Authorize(Roles = UserRoles.Buyer)]
    public async Task<IActionResult> ReadMessage(Guid id, ReadOrderMessageDto request)
    {
        await _service.MarkMessageReadAsync(id, CurrentUserId(), false, request.MessageAt);
        return NoContent();
    }

    private Guid CurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                value,
                out var id))
        {
            throw new UnauthorizedAccessException(
                "Invalid authenticated user.");
        }

        return id;
    }


    // =========================================================
    // AUTHENTICATED USER ROLE
    // =========================================================

    private string CurrentUserRole()
    {
        var role =
            User.FindFirstValue(
                ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(
                role))
        {
            throw new UnauthorizedAccessException(
                "Authenticated user role was not found.");
        }

        return role;
    }
}