using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ExportOfficerService : IExportOfficerService
{
    private readonly ApplicationDbContext _context;

    public ExportOfficerService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // 1. GET REVIEW QUEUE
    // ==========================================
    public async Task<ExportOfficerOperationResult> GetReviewQueueAsync(Guid officerUserId)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        var queueStatuses = new[]
        {
            ExportRequestStatus.Submitted,
            ExportRequestStatus.UnderComplianceReview,
            ExportRequestStatus.UnderOfficerReview
        };

        var requests = await _context.ExportRequests
            .AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .Where(r => queueStatuses.Contains(r.Status))
            .OrderBy(r => r.SubmittedAt)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        var mapped = requests.Select(MapToOfficerExportRequestResponseDto).ToList();

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = "Export review queue retrieved successfully.",
            Requests = mapped
        };
    }

    // ==========================================
    // 2. GET REQUEST FOR REVIEW
    // ==========================================
    public async Task<ExportOfficerOperationResult> GetRequestForReviewAsync(
        Guid officerUserId,
        Guid exportRequestId)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .FirstOrDefaultAsync(r => r.Id == exportRequestId);

        if (request == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status == ExportRequestStatus.Draft ||
            request.Status == ExportRequestStatus.Cancelled)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "This export request is not available for officer review.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = "Export request retrieved successfully.",
            Request = MapToOfficerExportRequestResponseDto(request)
        };
    }

    // ==========================================
    // 3. START REVIEW
    // ==========================================
    public async Task<ExportOfficerOperationResult> StartReviewAsync(
        Guid officerUserId,
        Guid exportRequestId)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .FirstOrDefaultAsync(r => r.Id == exportRequestId);

        if (request == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status == ExportRequestStatus.UnderOfficerReview)
        {
            return new ExportOfficerOperationResult
            {
                Success = true,
                Message = "Export request is already under officer review.",
                Request = MapToOfficerExportRequestResponseDto(request)
            };
        }

        if (request.Status != ExportRequestStatus.Submitted &&
            request.Status != ExportRequestStatus.UnderComplianceReview)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request cannot enter officer review from its current status.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        request.Status = ExportRequestStatus.UnderOfficerReview;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = "Export request review started successfully.",
            Request = MapToOfficerExportRequestResponseDto(request)
        };
    }

    // ==========================================
    // 4. MAKE DECISION
    // ==========================================
    public async Task<ExportOfficerOperationResult> MakeDecisionAsync(
        Guid officerUserId,
        Guid exportRequestId,
        ExportDecisionDto dto)
    {
        var officerValidation = await ValidateOfficerAsync(officerUserId);
        if (officerValidation != null)
        {
            return officerValidation;
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.Decision))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Decision must be Approve, Reject, or RequestRevision.",
                ErrorCode = "INVALID_DECISION"
            };
        }

        var decision = dto.Decision.Trim();
        ExportRequestStatus newStatus;
        string successMessage;

        if (decision.Equals("Approve", StringComparison.OrdinalIgnoreCase))
        {
            newStatus = ExportRequestStatus.Approved;
            successMessage = "Export request approved successfully.";
        }
        else if (decision.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            newStatus = ExportRequestStatus.Rejected;
            successMessage = "Export request rejected successfully.";
        }
        else if (decision.Equals("RequestRevision", StringComparison.OrdinalIgnoreCase))
        {
            newStatus = ExportRequestStatus.RevisionRequired;
            successMessage = "Revision requested successfully.";
        }
        else
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Decision must be Approve, Reject, or RequestRevision.",
                ErrorCode = "INVALID_DECISION"
            };
        }

        var normalizedReviewNotes = string.IsNullOrWhiteSpace(dto.ReviewNotes)
            ? null
            : dto.ReviewNotes.Trim();

        if (newStatus == ExportRequestStatus.Rejected && string.IsNullOrWhiteSpace(normalizedReviewNotes))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Review notes are required when rejecting an export request.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (newStatus == ExportRequestStatus.RevisionRequired && string.IsNullOrWhiteSpace(normalizedReviewNotes))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Review notes are required when requesting a revision.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .Include(r => r.RequestedByUser)
            .Include(r => r.ComplianceDocuments)
            .FirstOrDefaultAsync(r => r.Id == exportRequestId);

        if (request == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status != ExportRequestStatus.UnderOfficerReview)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Final decisions can only be made for requests under officer review.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        var now = DateTime.UtcNow;
        request.Status = newStatus;
        request.ReviewedByUserId = officerUserId;
        request.ReviewedAt = now;
        request.UpdatedAt = now;
        request.ReviewNotes = normalizedReviewNotes;

        await _context.SaveChangesAsync();

        return new ExportOfficerOperationResult
        {
            Success = true,
            Message = successMessage,
            Request = MapToOfficerExportRequestResponseDto(request)
        };
    }

    // ==========================================
    // PRIVATE OFFICER VALIDATION HELPER
    // ==========================================
    private async Task<ExportOfficerOperationResult?> ValidateOfficerAsync(Guid officerUserId)
    {
        if (officerUserId == Guid.Empty)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        var officer = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == officerUserId);

        if (officer == null)
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "Export officer account was not found.",
                ErrorCode = "OFFICER_NOT_FOUND"
            };
        }

        if (!officer.Role.Equals(UserRoles.ExportOfficer, StringComparison.OrdinalIgnoreCase))
        {
            return new ExportOfficerOperationResult
            {
                Success = false,
                Message = "You are not authorized to perform export officer operations.",
                ErrorCode = "FORBIDDEN"
            };
        }

        return null;
    }

    // ==========================================
    // PRIVATE MAPPING HELPER
    // ==========================================
    private static OfficerExportRequestResponseDto MapToOfficerExportRequestResponseDto(ExportRequest entity)
    {
        var mappedDocuments = entity.ComplianceDocuments?
            .Select(d => new ComplianceDocumentResponseDto
            {
                Id = d.Id,
                ExportRequestId = d.ExportRequestId,
                DocumentType = d.DocumentType,
                DocumentNumber = d.DocumentNumber,
                Issuer = d.Issuer,
                IssueDate = d.IssueDate,
                ExpiryDate = d.ExpiryDate,
                FileUrl = d.FileUrl,
                Status = d.Status.ToString(),
                UploadedAt = d.UploadedAt
            })
            .ToList() ?? new List<ComplianceDocumentResponseDto>();

        return new OfficerExportRequestResponseDto
        {
            Id = entity.Id,
            RequestedByUserId = entity.RequestedByUserId,
            RequesterName = entity.RequestedByUser?.FullName ?? string.Empty,
            RequesterEmail = entity.RequestedByUser?.Email ?? string.Empty,
            OriginCountry = entity.OriginCountry,
            DestinationCountry = entity.DestinationCountry,
            DeclaredValue = entity.DeclaredValue,
            Currency = entity.Currency,
            Purpose = entity.Purpose,
            Status = entity.Status.ToString(),
            ReviewNotes = entity.ReviewNotes,
            SubmittedAt = entity.SubmittedAt,
            ReviewedAt = entity.ReviewedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Documents = mappedDocuments
        };
    }
}
