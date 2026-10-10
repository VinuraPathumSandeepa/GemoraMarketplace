using System.Security.Claims;

using Gemora.Application.DTOs.MarketplaceAgent;
using Gemora.Application.Interfaces;

using Gemora.Domain.Constants;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Gemora.API.Controllers;


[ApiController]
[Route("api/marketplace/agent")]
[Authorize(Roles = UserRoles.Buyer)]
public class MarketplaceAgentController
    : ControllerBase
{
    private readonly IMarketplaceAgentService
        _marketplaceAgentService;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MarketplaceAgentController(
        IMarketplaceAgentService marketplaceAgentService)
    {
        _marketplaceAgentService =
            marketplaceAgentService;
    }


    // ============================================================
    // GEMINI MARKETPLACE ASSISTANT
    //
    // POST:
    // /api/marketplace/agent/assist
    //
    // Example body:
    //
    // {
    //   "message": "Show me blue sapphires under LKR 500000",
    //   "conversationId": null
    // }
    // ============================================================

    [HttpPost("assist")]
    public async Task<
        ActionResult<MarketplaceAgentResponseDto>>
        Assist(
            [FromBody]
            MarketplaceAgentRequestDto request,

            CancellationToken cancellationToken)
    {
        if (
            request == null ||
            string.IsNullOrWhiteSpace(
                request.Message)
        )
        {
            return BadRequest(
                new
                {
                    message =
                        "Please enter a message for the marketplace assistant."
                }
            );
        }


        var buyerId =
            GetCurrentUserId();


        var response =
            await _marketplaceAgentService
                .AskAsync(
                    buyerId,
                    request,
                    cancellationToken
                );


        return Ok(
            response
        );
    }


    // ============================================================
    // CURRENT AUTHENTICATED BUYER ID
    //
    // IMPORTANT:
    // Buyer ID comes from JWT.
    // It does NOT come from request body.
    // ============================================================

    private Guid GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (
            !Guid.TryParse(
                value,
                out var userId)
        )
        {
            throw new UnauthorizedAccessException(
                "The authenticated user ID is invalid."
            );
        }


        return userId;
    }
}