using System.Security.Claims;
using Gemora.Application.DTOs.GemListings;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Seller)]
public class GemListingsController : ControllerBase
{
    private readonly IGemListingService _gemListingService;

    public GemListingsController(
        IGemListingService gemListingService)
    {
        _gemListingService = gemListingService;
    }

    // ============================================================
    // CREATE GEM LISTING
    // POST: /api/GemListings
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateGemListingDto dto)
    {
        var sellerId = GetCurrentUserId();

        var listing =
            await _gemListingService.CreateAsync(
                sellerId,
                dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = listing.Id },
            listing);
    }

    // ============================================================
    // GET ALL LISTINGS OF CURRENT SELLER
    // GET: /api/GemListings/my
    // ============================================================

    [HttpGet("my")]
    public async Task<IActionResult> GetMyListings()
    {
        var sellerId = GetCurrentUserId();

        var listings =
            await _gemListingService.GetMyListingsAsync(
                sellerId);

        return Ok(listings);
    }

    // ============================================================
    // GET ONE LISTING OF CURRENT SELLER
    // GET: /api/GemListings/{id}
    // ============================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sellerId = GetCurrentUserId();

        var listing =
            await _gemListingService.GetByIdAsync(
                id,
                sellerId);

        if (listing == null)
        {
            return NotFound(new
            {
                message = "Gem listing not found."
            });
        }

        return Ok(listing);
    }

    // ============================================================
    // UPDATE GEM LISTING
    // PUT: /api/GemListings/{id}
    // ============================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateGemListingDto dto)
    {
        var sellerId = GetCurrentUserId();

        var updated =
            await _gemListingService.UpdateAsync(
                id,
                sellerId,
                dto);

        if (!updated)
        {
            return NotFound(new
            {
                message = "Gem listing not found."
            });
        }

        return Ok(new
        {
            message = "Gem listing updated successfully."
        });
    }

    // ============================================================
    // DELETE GEM LISTING
    // DELETE: /api/GemListings/{id}
    // ============================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var sellerId = GetCurrentUserId();

        var deleted =
            await _gemListingService.DeleteAsync(
                id,
                sellerId);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Gem listing not found."
            });
        }

        return Ok(new
        {
            message = "Gem listing deleted successfully."
        });
    }

    // ============================================================
    // SUBMIT GEM LISTING FOR VERIFICATION
    // POST: /api/GemListings/{id}/submit-verification
    // ============================================================

    [HttpPost("{id:int}/submit-verification")]
    public async Task<IActionResult> SubmitForVerification(
        int id)
    {
        var sellerId = GetCurrentUserId();

        var listing =
            await _gemListingService
                .SubmitForVerificationAsync(
                    id,
                    sellerId);

        if (listing == null)
        {
            return NotFound(new
            {
                message = "Gem listing not found."
            });
        }

        return Ok(new
        {
            message =
                "Gem listing submitted for verification successfully.",

            listing
        });
    }

    // ============================================================
    // GET CURRENT AUTHENTICATED USER ID FROM JWT
    // ============================================================

    private Guid GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId) ||
            !Guid.TryParse(userId, out var id))
        {
            throw new UnauthorizedAccessException(
                "Invalid authenticated user.");
        }

        return id;
    }
}