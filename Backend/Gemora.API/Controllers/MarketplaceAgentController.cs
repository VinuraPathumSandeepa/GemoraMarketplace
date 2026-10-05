using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/marketplace/agent")]
[Authorize(Roles = UserRoles.Buyer)]
public class MarketplaceAgentController : ControllerBase
{
    private readonly IMarketplaceAgentService _service;
    public MarketplaceAgentController(IMarketplaceAgentService service) => _service = service;

    [HttpPost("assist")]
    public async Task<ActionResult<MarketplaceAgentResponseDto>> Assist(MarketplaceAgentRequestDto request)
        => Ok(await _service.AssistAsync(request));
}
