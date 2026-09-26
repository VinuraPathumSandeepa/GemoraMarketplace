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
    private readonly IGemVerificationAgent _gemVerificationAgent;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public GemVerificationsController(
        IGemVerificationService gemVerificationService,
        IGemVerificationAgent gemVerificationAgent)
    {
        _gemVerificationService =
            gemVerificationService;

        _gemVerificationAgent =
            gemVerificationAgent;
    }


    // ============================================================
    // GET PENDING VERIFICATIONS
    //
    // GET:
    // /api/GemVerifications/pending
    //
    // Gemologist only.
    // ============================================================

    [HttpGet("pending")]
    public async Task<ActionResult<List<GemVerificationDto>>>
        GetPendingVerifications()
    {
        var verifications =
            await _gemVerificationService
                .GetPendingVerificationsAsync();

        return Ok(verifications);
    }


    // ============================================================
    // GET VERIFICATION BY ID
    //
    // GET:
    // /api/GemVerifications/{verificationId}
    //
    // Example:
    // /api/GemVerifications/5
    // ============================================================

    [HttpGet("{verificationId:int}")]
    public async Task<ActionResult<GemVerificationDto>>
        GetVerificationById(
            int verificationId)
    {
        var verification =
            await _gemVerificationService
                .GetVerificationByIdAsync(
                    verificationId);

        if (verification == null)
        {
            return NotFound(
                new
                {
                    message =
                        "Gem verification was not found."
                });
        }

        return Ok(verification);
    }


    // ============================================================
    // RUN AI-ASSISTED GEM ANALYSIS
    //
    // POST:
    // /api/GemVerifications/{verificationId}/ai-analysis
    //
    // Example:
    // /api/GemVerifications/5/ai-analysis
    //
    // Current agent workflow:
    //
    // NotStarted
    //      ↓
    // Processing
    //      ↓
    // Deterministic evidence validation
    //      ↓
    // ┌──────────────────────┬─────────────────────────┐
    // │ Validation fails     │ Validation succeeds     │
    // ↓                      ↓
    // NeedsMoreEvidence      AwaitingModelAnalysis
    //
    // The actual external AI model will be connected in
    // the next implementation stage.
    //
    // IMPORTANT:
    // AI does not approve/reject the gemstone.
    // Final authority remains with the Gemologist.
    // ============================================================

    [HttpPost("{verificationId:int}/ai-analysis")]
    public async Task<IActionResult> RunAiAnalysis(
        int verificationId)
    {
        var result =
            await _gemVerificationAgent
                .AnalyzeAsync(
                    verificationId);

        return Ok(result);
    }


    // ============================================================
    // HUMAN GEMOLOGIST REVIEW
    //
    // PUT:
    // /api/GemVerifications/{verificationId}/review
    //
    // Supported decisions:
    //
    // Approved
    // ChangesRequested
    // Rejected
    //
    // The AI analysis is advisory only.
    // This endpoint represents the human approval/review step.
    // ============================================================

    [HttpPut("{verificationId:int}/review")]
    public async Task<ActionResult<GemVerificationDto>>
        ReviewVerification(
            int verificationId,
            [FromBody] ReviewGemVerificationDto dto)
    {
        var gemologistId =
            GetCurrentUserId();

        var verification =
            await _gemVerificationService
                .ReviewVerificationAsync(
                    verificationId,
                    gemologistId,
                    dto);

        if (verification == null)
        {
            return NotFound(
                new
                {
                    message =
                        "Gem verification was not found."
                });
        }

        return Ok(verification);
    }


    // ============================================================
    // CURRENT USER ID
    //
    // JWT NameIdentifier contains the Gemora User Guid.
    // ============================================================

    private Guid GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                userIdValue))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user identifier is missing.");
        }


        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user identifier is invalid.");
        }


        return userId;
    }
}