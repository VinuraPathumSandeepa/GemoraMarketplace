using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/marketplace")]
[AllowAnonymous]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _service;

    public MarketplaceController(IMarketplaceService service)
    {
        _service = service;
    }

    [HttpGet("gems")]
    public async Task<ActionResult<PagedMarketplaceResponseDto>> Search(
        [FromQuery] MarketplaceSearchQueryDto query)
    {
        return Ok(await _service.SearchAsync(query));
    }

    [HttpGet("gems/{id:int}")]
    public async Task<ActionResult<MarketplaceGemResponseDto>> GetById(int id)
    {
        var gem = await _service.GetByIdAsync(id);

        return gem is null
            ? NotFound(new { message = "Marketplace gem was not found or is unavailable." })
            : Ok(gem);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<MarketplaceStatsDto>> GetStats()
    {
        return Ok(await _service.GetStatsAsync());
    }
}
