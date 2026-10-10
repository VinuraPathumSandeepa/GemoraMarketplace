using Gemora.Application.DTOs.Orders;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/admin/transactions")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminTransactionsController : ControllerBase
{
    private readonly IOrderService _service;
    public AdminTransactionsController(IOrderService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAll()
        => Ok(await _service.GetAllForAdminAsync());
}
