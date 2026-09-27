using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ExportComplianceService : IExportComplianceService
{
    private readonly ApplicationDbContext _context;

    public ExportComplianceService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==========================================
    // CREATE EXPORT REQUEST
    // ==========================================
    public async Task<ExportRequestOperationResult> CreateExportRequestAsync(
        Guid userId,
        CreateExportRequestDto dto)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user was not found.",
                ErrorCode = "USER_NOT_FOUND"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.OriginCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Origin country is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.DestinationCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Destination country is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (dto.DeclaredValue <= 0)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Declared value must be greater than 0.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.Currency))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Currency is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var now = DateTime.UtcNow;
        var exportRequest = new ExportRequest
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = userId,
            OriginCountry = dto.OriginCountry.Trim(),
            DestinationCountry = dto.DestinationCountry.Trim(),
            DeclaredValue = dto.DeclaredValue,
            Currency = dto.Currency.Trim().ToUpperInvariant(),
            Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
            Status = ExportRequestStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.ExportRequests.Add(exportRequest);
        await _context.SaveChangesAsync();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request created successfully.",
            Request = MapToResponseDto(exportRequest)
        };
    }

    // ==========================================
    // GET MY EXPORT REQUESTS
    // ==========================================
    public async Task<ExportRequestOperationResult> GetMyExportRequestsAsync(
        Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        var requests = await _context.ExportRequests
            .AsNoTracking()
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var mapped = requests.Select(MapToResponseDto).ToList();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export requests retrieved successfully.",
            Requests = mapped
        };
    }

    // ==========================================
    // GET EXPORT REQUEST BY ID
    // ==========================================
    public async Task<ExportRequestOperationResult> GetExportRequestByIdAsync(
        Guid userId,
        Guid exportRequestId)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (request == null)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request retrieved successfully.",
            Details = MapToDetailsDto(request)
        };
    }

    // ==========================================
    // UPDATE EXPORT REQUEST
    // ==========================================
    public async Task<ExportRequestOperationResult> UpdateExportRequestAsync(
        Guid userId,
        Guid exportRequestId,
        UpdateExportRequestDto dto)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (request == null)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status != ExportRequestStatus.Draft &&
            request.Status != ExportRequestStatus.RevisionRequired)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Only draft or revision-required export requests can be edited.",
                ErrorCode = "INVALID_STATUS"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.OriginCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Origin country is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.DestinationCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Destination country is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (dto.DeclaredValue <= 0)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Declared value must be greater than 0.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.Currency))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Currency is required.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        request.OriginCountry = dto.OriginCountry.Trim();
        request.DestinationCountry = dto.DestinationCountry.Trim();
        request.DeclaredValue = dto.DeclaredValue;
        request.Currency = dto.Currency.Trim().ToUpperInvariant();
        request.Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request updated successfully.",
            Request = MapToResponseDto(request)
        };
    }

    // ==========================================
    // SUBMIT EXPORT REQUEST
    // ==========================================
    public async Task<ExportRequestOperationResult> SubmitExportRequestAsync(
        Guid userId,
        Guid exportRequestId)
    {
        if (userId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        if (exportRequestId == Guid.Empty)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var request = await _context.ExportRequests
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (request == null)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        if (request.Status != ExportRequestStatus.Draft &&
            request.Status != ExportRequestStatus.RevisionRequired)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "This export request cannot be submitted in its current status.",
                ErrorCode = "SUBMISSION_NOT_ALLOWED"
            };
        }

        if (string.IsNullOrWhiteSpace(request.OriginCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Origin country is required for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(request.DestinationCountry))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Destination country is required for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (request.DeclaredValue <= 0)
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Declared value must be greater than 0 for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            return new ExportRequestOperationResult
            {
                Success = false,
                Message = "Currency is required for submission.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var now = DateTime.UtcNow;
        request.Status = ExportRequestStatus.Submitted;
        request.SubmittedAt = now;
        request.UpdatedAt = now;

        await _context.SaveChangesAsync();

        return new ExportRequestOperationResult
        {
            Success = true,
            Message = "Export request submitted successfully.",
            Request = MapToResponseDto(request)
        };
    }

    // ==========================================
    // MAPPING HELPERS
    // ==========================================
    private static ExportRequestResponseDto MapToResponseDto(ExportRequest entity)
    {
        return new ExportRequestResponseDto
        {
            Id = entity.Id,
            OriginCountry = entity.OriginCountry,
            DestinationCountry = entity.DestinationCountry,
            DeclaredValue = entity.DeclaredValue,
            Currency = entity.Currency,
            Purpose = entity.Purpose,
            Status = entity.Status.ToString(),
            SubmittedAt = entity.SubmittedAt,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static ExportRequestDetailsDto MapToDetailsDto(ExportRequest entity)
    {
        return new ExportRequestDetailsDto
        {
            Id = entity.Id,
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
            UpdatedAt = entity.UpdatedAt
        };
    }
}
