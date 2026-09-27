using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportRequestsController : ControllerBase
{
    private readonly IExportComplianceService _exportComplianceService;

    public ExportRequestsController(IExportComplianceService exportComplianceService)
    {
        _exportComplianceService = exportComplianceService;
    }

    // ==========================================
    // 1. CREATE EXPORT REQUEST
    // POST: /api/ExportRequests
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> CreateExportRequest(CreateExportRequestDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportComplianceService.CreateExportRequestAsync(userId, dto);

        if (!result.Success)
        {
            if (result.ErrorCode == "INVALID_USER" || result.ErrorCode == "USER_NOT_FOUND")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message = result.Message,
                request = result.Request
            }
        );
    }

    // ==========================================
    // 2. GET MY EXPORT REQUESTS
    // GET: /api/ExportRequests/my
    // ==========================================
    [HttpGet("my")]
    public async Task<IActionResult> GetMyExportRequests()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportComplianceService.GetMyExportRequestsAsync(userId);

        if (!result.Success)
        {
            if (result.ErrorCode == "INVALID_USER")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,
            requests = result.Requests
        });
    }

    // ==========================================
    // 3. GET EXPORT REQUEST BY ID
    // GET: /api/ExportRequests/{id}
    // ==========================================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetExportRequestById(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportComplianceService.GetExportRequestByIdAsync(userId, id);

        if (!result.Success)
        {
            if (result.ErrorCode == "REQUEST_NOT_FOUND")
            {
                return NotFound(new
                {
                    message = result.Message
                });
            }

            if (result.ErrorCode == "INVALID_USER")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,
            request = result.Details
        });
    }

    // ==========================================
    // 4. UPDATE EXPORT REQUEST
    // PUT: /api/ExportRequests/{id}
    // ==========================================
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExportRequest(Guid id, UpdateExportRequestDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportComplianceService.UpdateExportRequestAsync(userId, id, dto);

        if (!result.Success)
        {
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

            if (result.ErrorCode == "INVALID_USER")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,
            request = result.Request
        });
    }

    // ==========================================
    // 5. SUBMIT EXPORT REQUEST
    // POST: /api/ExportRequests/{id}/submit
    // ==========================================
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitExportRequest(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportComplianceService.SubmitExportRequestAsync(userId, id);

        if (!result.Success)
        {
            if (result.ErrorCode == "REQUEST_NOT_FOUND")
            {
                return NotFound(new
                {
                    message = result.Message
                });
            }

            if (result.ErrorCode == "SUBMISSION_NOT_ALLOWED")
            {
                return Conflict(new
                {
                    message = result.Message
                });
            }

            if (result.ErrorCode == "INVALID_USER")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,
            request = result.Request
        });
    }

    // ==========================================
    // 6. ADD COMPLIANCE DOCUMENT
    // POST: /api/ExportRequests/{id}/documents
    // ==========================================
    [HttpPost("{id}/documents")]
    public async Task<IActionResult> AddComplianceDocument(Guid id, CreateComplianceDocumentDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportComplianceService.AddComplianceDocumentAsync(
            userId,
            id,
            dto
        );

        if (!result.Success)
        {
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

            if (result.ErrorCode == "INVALID_USER")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message = result.Message,
                document = result.Document
            }
        );
    }

    // ==========================================
    // 7. GET COMPLIANCE DOCUMENTS
    // GET: /api/ExportRequests/{id}/documents
    // ==========================================
    [HttpGet("{id}/documents")]
    public async Task<IActionResult> GetComplianceDocuments(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized(new
            {
                message = "Authenticated user is invalid."
            });
        }

        var result = await _exportComplianceService.GetComplianceDocumentsAsync(
            userId,
            id
        );

        if (!result.Success)
        {
            if (result.ErrorCode == "REQUEST_NOT_FOUND")
            {
                return NotFound(new
                {
                    message = result.Message
                });
            }

            if (result.ErrorCode == "INVALID_USER")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,
            documents = result.Documents
        });
    }

    // ==========================================
    // PRIVATE HELPER: AUTHENTICATED USER ID
    // ==========================================
    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(claimValue) && Guid.TryParse(claimValue, out userId))
        {
            return true;
        }

        userId = Guid.Empty;
        return false;
    }
}
