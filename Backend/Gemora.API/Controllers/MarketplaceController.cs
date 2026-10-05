using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


using Microsoft.AspNetCore.Authorization;
namespace Gemora.API.Controllers;

[ApiController]
[Route("api/marketplace")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _service;
    public MarketplaceController(IMarketplaceService service) => _service = service;

    [AllowAnonymous]
    [HttpGet("gems")]
    public async Task<ActionResult<PagedMarketplaceResponseDto>> Search([FromQuery] MarketplaceSearchQueryDto query)
        => Ok(await _service.SearchAsync(query));

    [AllowAnonymous]
    [HttpGet("gems/{id:int}")]
    public async Task<ActionResult<MarketplaceGemResponseDto>> GetById(int id)
    {
        var gem = await _service.GetByIdAsync(id);
        return gem == null ? NotFound(new { message = "Marketplace gem was not found or is unavailable." }) : Ok(gem);
    }


    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats()
    {
        return Ok(
            await _service.GetStatsAsync()
        );
    }
}
