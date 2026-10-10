using System.Security.Claims;
using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/export-officer")]
[Authorize(Roles = UserRoles.ExportOfficer)]
public class ExportOfficerController : ControllerBase
{
    private readonly IExportOfficerService _exportOfficerService;

    public ExportOfficerController(IExportOfficerService exportOfficerService)
    {
        _exportOfficerService = exportOfficerService;
    }

    // ==========================================
    // 1. GET REVIEW QUEUE
    // GET: /api/export-officer/requests
    // ==========================================
    [HttpGet("requests")]
    public async Task<IActionResult> GetReviewQueue()
    {
        if (!TryGetCurrentOfficerId(out var officerUserId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportOfficerService.GetReviewQueueAsync(officerUserId);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            message = result.Message,
            requests = result.Requests
        });
    }

    // ==========================================
    // 2. GET REQUEST FOR REVIEW
    // GET: /api/export-officer/requests/{id}
    // ==========================================
    [HttpGet("requests/{id}")]
    public async Task<IActionResult> GetRequestForReview(Guid id)
    {
        if (!TryGetCurrentOfficerId(out var officerUserId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportOfficerService.GetRequestForReviewAsync(officerUserId, id);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            message = result.Message,
            request = result.Request
        });
    }

    // ==========================================
    // 3. START REVIEW
    // POST: /api/export-officer/requests/{id}/start-review
    // ==========================================
    [HttpPost("requests/{id}/start-review")]
    public async Task<IActionResult> StartReview(Guid id)
    {
        if (!TryGetCurrentOfficerId(out var officerUserId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportOfficerService.StartReviewAsync(officerUserId, id);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            message = result.Message,
            request = result.Request
        });
    }

    // ==========================================
    // 4. MAKE FINAL DECISION
    // POST: /api/export-officer/requests/{id}/decision
    // ==========================================
    [HttpPost("requests/{id}/decision")]
    public async Task<IActionResult> MakeDecision(Guid id, ExportDecisionDto dto)
    {
        if (!TryGetCurrentOfficerId(out var officerUserId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportOfficerService.MakeDecisionAsync(officerUserId, id, dto);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            message = result.Message,
            request = result.Request
        });
    }

    // ==========================================
    // 5. DOWNLOAD COMPLIANCE DOCUMENT FILE
    // GET: /api/export-officer/requests/{id}/documents/{documentId}/file
    // ==========================================
    [HttpGet("requests/{id}/documents/{documentId}/file")]
    public async Task<IActionResult> DownloadComplianceDocumentFile(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentOfficerId(out var officerUserId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportOfficerService.GetDocumentFileForReviewAsync(
            officerUserId,
            id,
            documentId,
            cancellationToken
        );

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "INVALID_USER" => Unauthorized(new { message = result.Message }),
                "OFFICER_NOT_FOUND" => Unauthorized(new { message = result.Message }),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Message }),
                "INVALID_REQUEST" => BadRequest(new { message = result.Message }),
                "REQUEST_NOT_FOUND" => NotFound(new { message = result.Message }),
                "DOCUMENT_NOT_FOUND" => NotFound(new { message = result.Message }),
                "FILE_NOT_UPLOADED" => NotFound(new { message = result.Message }),
                "FILE_NOT_FOUND" => NotFound(new { message = result.Message }),
                "INVALID_STATUS" => Conflict(new { message = result.Message }),
                "UNSUPPORTED_FILE_TYPE" => BadRequest(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message })
            };
        }

        if (result.Content == null ||
            string.IsNullOrWhiteSpace(result.ContentType) ||
            string.IsNullOrWhiteSpace(result.DownloadFileName))
        {
            if (result.Content != null)
            {
                await result.Content.DisposeAsync();
            }

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "The compliance document could not be prepared for download."
                }
            );
        }

        return File(
            result.Content,
            result.ContentType,
            result.DownloadFileName
        );
    }

    // ==========================================
    // PRIVATE USER ID HELPER
    // ==========================================
    private bool TryGetCurrentOfficerId(out Guid officerUserId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(claimValue) && Guid.TryParse(claimValue, out officerUserId))
        {
            return true;
        }

        officerUserId = Guid.Empty;
        return false;
    }

    // ==========================================
    // PRIVATE ERROR MAPPING HELPER
    // ==========================================
    private IActionResult MapError(ExportOfficerOperationResult result)
    {
        if (result.ErrorCode == "INVALID_USER" || result.ErrorCode == "OFFICER_NOT_FOUND")
        {
            return Unauthorized(new
            {
                message = result.Message
            });
        }

        if (result.ErrorCode == "FORBIDDEN")
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = result.Message
                }
            );
        }

        if (result.ErrorCode == "REQUEST_NOT_FOUND")
        {
            return NotFound(new
            {
                message = result.Message
            });
        }

        if (result.ErrorCode == "INVALID_STATUS")
        {
            return Conflict(new
            {
                message = result.Message
            });
        }

        if (result.ErrorCode == "INVALID_REQUEST" || result.ErrorCode == "INVALID_DECISION")
        {
            return BadRequest(new
            {
                message = result.Message
            });
        }

        return BadRequest(new
        {
            message = result.Message
        });
    }
}
