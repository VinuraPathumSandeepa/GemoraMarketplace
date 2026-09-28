using System.Security.Claims;
using Gemora.API.Models.Uploads;
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
    //
    // POST /api/GemListings
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<GemListingDto>> Create(
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
    // GET CURRENT SELLER'S LISTINGS
    //
    // GET /api/GemListings/my
    // ============================================================

    [HttpGet("my")]
    public async Task<ActionResult<List<GemListingDto>>>
        GetMyListings()
    {
        var sellerId = GetCurrentUserId();

        var listings =
            await _gemListingService
                .GetMyListingsAsync(
                    sellerId);

        return Ok(listings);
    }


    // ============================================================
    // GET ONE LISTING
    //
    // GET /api/GemListings/{id}
    //
    // The service also checks ownership.
    // ============================================================

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GemListingDto>>
        GetById(int id)
    {
        var sellerId = GetCurrentUserId();

        var listing =
            await _gemListingService
                .GetByIdAsync(
                    id,
                    sellerId);

        if (listing == null)
        {
            return NotFound(new
            {
                message =
                    "Gem listing was not found."
            });
        }

        return Ok(listing);
    }


    // ============================================================
    // UPDATE LISTING
    //
    // PUT /api/GemListings/{id}
    //
    // Only Draft / ChangesRequested listings can be edited.
    // ============================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateGemListingDto dto)
    {
        var sellerId = GetCurrentUserId();

        var updated =
            await _gemListingService
                .UpdateAsync(
                    id,
                    sellerId,
                    dto);

        if (!updated)
        {
            return NotFound(new
            {
                message =
                    "Gem listing was not found."
            });
        }

        return Ok(new
        {
            message =
                "Gem listing updated successfully."
        });
    }


    // ============================================================
    // DELETE LISTING
    //
    // DELETE /api/GemListings/{id}
    //
    // Only Draft listings can be deleted.
    // ============================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id)
    {
        var sellerId = GetCurrentUserId();

        var deleted =
            await _gemListingService
                .DeleteAsync(
                    id,
                    sellerId);

        if (!deleted)
        {
            return NotFound(new
            {
                message =
                    "Gem listing was not found."
            });
        }

        return Ok(new
        {
            message =
                "Gem listing deleted successfully."
        });
    }


    // ============================================================
    // UPLOAD / REPLACE PRIMARY GEM IMAGE
    //
    // POST /api/GemListings/{id}/image
    //
    // multipart/form-data
    //
    // Allowed by LocalFileStorageService:
    // JPG
    // JPEG
    // PNG
    // WEBP
    //
    // Maximum size:
    // 5 MB
    // ============================================================

    [HttpPost("{id:int}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<GemListingDto>>
        UploadGemImage(
            int id,
            [FromForm] GemImageUploadRequest request)
    {
        if (request.File == null ||
            request.File.Length == 0)
        {
            return BadRequest(new
            {
                message =
                    "Please select a gemstone image to upload."
            });
        }

        var sellerId =
            GetCurrentUserId();

        await using var fileStream =
            request.File.OpenReadStream();

        var listing =
            await _gemListingService
                .UploadGemImageAsync(
                    id,
                    sellerId,
                    fileStream,
                    request.File.FileName,
                    request.File.ContentType,
                    request.File.Length);

        if (listing == null)
        {
            return NotFound(new
            {
                message =
                    "Gem listing was not found."
            });
        }

        return Ok(listing);
    }


    // ============================================================
    // UPLOAD / REPLACE CERTIFICATE
    //
    // POST /api/GemListings/{id}/certificate
    //
    // multipart/form-data
    //
    // Allowed by LocalFileStorageService:
    // PDF
    // JPG
    // JPEG
    // PNG
    //
    // Maximum size:
    // 10 MB
    // ============================================================

    [HttpPost("{id:int}/certificate")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<GemListingDto>>
        UploadCertificate(
            int id,
            [FromForm] CertificateUploadRequest request)
    {
        if (request.File == null ||
            request.File.Length == 0)
        {
            return BadRequest(new
            {
                message =
                    "Please select a certificate file to upload."
            });
        }

        var sellerId =
            GetCurrentUserId();

        await using var fileStream =
            request.File.OpenReadStream();

        var listing =
            await _gemListingService
                .UploadCertificateAsync(
                    id,
                    sellerId,
                    fileStream,
                    request.File.FileName,
                    request.File.ContentType,
                    request.File.Length);

        if (listing == null)
        {
            return NotFound(new
            {
                message =
                    "Gem listing was not found."
            });
        }

        return Ok(listing);
    }


    // ============================================================
    // SUBMIT LISTING FOR VERIFICATION
    //
    // POST /api/GemListings/{id}/submit-verification
    //
    // Draft
    //      ↓
    // PendingVerification
    //
    // OR
    //
    // ChangesRequested
    //      ↓
    // PendingVerification
    //
    // A new GemVerification record is created for every
    // submission/resubmission.
    // ============================================================

    [HttpPost("{id:int}/submit-verification")]
    public async Task<ActionResult<GemListingDto>>
        SubmitForVerification(
            int id)
    {
        var sellerId =
            GetCurrentUserId();

        var listing =
            await _gemListingService
                .SubmitForVerificationAsync(
                    id,
                    sellerId);

        if (listing == null)
        {
            return NotFound(new
            {
                message =
                    "Gem listing was not found."
            });
        }

        return Ok(listing);
    }


    // ============================================================
    // GET AUTHENTICATED USER ID
    //
    // JWT NameIdentifier contains User.Id (Guid).
    // ============================================================

    private Guid GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user ID is invalid.");
        }

        return userId;
    }
}