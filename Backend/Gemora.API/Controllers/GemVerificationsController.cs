using System.Security.Claims;
using Gemora.Application.DTOs.GemVerifications;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Gemologist)]
public class GemVerificationsController : ControllerBase
{
    private readonly IGemVerificationService _gemVerificationService;

    public GemVerificationsController(
        IGemVerificationService gemVerificationService)
    {
        _gemVerificationService = gemVerificationService;
    }


    // ============================================================
    // GET PENDING VERIFICATION QUEUE
    //
    // GET /api/GemVerifications/pending
    // ============================================================

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingVerifications()
    {
        var verifications =
            await _gemVerificationService
                .GetPendingVerificationsAsync();

        return Ok(verifications);
    }


    // ============================================================
    // GET ONE VERIFICATION
    //
    // GET /api/GemVerifications/{id}
    // ============================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetVerificationById(int id)
    {
        var verification =
            await _gemVerificationService
                .GetVerificationByIdAsync(id);

        if (verification == null)
        {
            return NotFound(new
            {
                message = "Gem verification not found."
            });
        }

        return Ok(verification);
    }


    // ============================================================
    // REVIEW A GEM VERIFICATION
    //
    // POST /api/GemVerifications/{id}/review
    //
    // Allowed decisions:
    // Approved
    // ChangesRequested
    // Rejected
    // ============================================================

    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> ReviewVerification(
        int id,
        [FromBody] ReviewGemVerificationDto dto)
    {
        // Get the logged-in Gemologist's Guid from the JWT.
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var gemologistId))
        {
            return Unauthorized(new
            {
                message = "Invalid authenticated user."
            });
        }

        var verification =
            await _gemVerificationService
                .ReviewVerificationAsync(
                    id,
                    gemologistId,
                    dto);

        if (verification == null)
        {
            return NotFound(new
            {
                message = "Gem verification not found."
            });
        }

        return Ok(verification);
    }
}