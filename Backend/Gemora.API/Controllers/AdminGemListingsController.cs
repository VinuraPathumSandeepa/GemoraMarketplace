using System.Security.Claims;
using Gemora.Application.DTOs.GemListings;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/admin/gem-listings")]
[Authorize(Roles = UserRoles.Admin)]
public class AdminGemListingsController : ControllerBase
{
    private readonly IGemListingService _service;

    public AdminGemListingsController(IGemListingService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<GemListingDto>>> GetAll()
        => Ok(await _service.GetAllForAdminAsync());

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawId, out var adminId)) return Unauthorized();

        if (!await _service.DeleteForAdminAsync(id, adminId))
            return NotFound(new { message = "Gem listing was not found." });

        return NoContent();
    }
}
