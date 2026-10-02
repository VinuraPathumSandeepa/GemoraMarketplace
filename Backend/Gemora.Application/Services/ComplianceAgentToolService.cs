using Gemora.Application.DTOs.Agent;
using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ComplianceAgentToolService : IComplianceAgentToolService
{
    private readonly ApplicationDbContext _context;
    private readonly IComplianceRulesService _complianceRulesService;

    public ComplianceAgentToolService(
        ApplicationDbContext context,
        IComplianceRulesService complianceRulesService)
    {
        _context = context;
        _complianceRulesService = complianceRulesService;
    }

    // ======================================================
    // 1. READ EXPORT REQUEST
    // ======================================================
    public async Task<AgentToolResult<ComplianceAgentExportRequestDto>> ReadExportRequestAsync(
        Guid exportRequestId,
        CancellationToken cancellationToken = default)
    {
        if (exportRequestId == Guid.Empty)
        {
            return new AgentToolResult<ComplianceAgentExportRequestDto>
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Export request ID is invalid."
            };
        }

        var request = await _context.ExportRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == exportRequestId, cancellationToken);

        if (request == null)
        {
            return new AgentToolResult<ComplianceAgentExportRequestDto>
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
            };
        }

        var mapped = new ComplianceAgentExportRequestDto
        {
            ExportRequestId = request.Id,
            OriginCountry = request.OriginCountry,
            DestinationCountry = request.DestinationCountry,
            DeclaredValue = request.DeclaredValue,
            Currency = request.Currency,
            Purpose = request.Purpose,
            Status = request.Status.ToString(),
            SubmittedAt = request.SubmittedAt
        };

        return new AgentToolResult<ComplianceAgentExportRequestDto>
        {
            Success = true,
            Message = "Export request retrieved successfully.",
            Data = mapped
        };
    }

    // ======================================================
    // 2. READ COMPLIANCE DOCUMENT METADATA
    // ======================================================
    public async Task<AgentToolResult<IReadOnlyList<ComplianceAgentDocumentDto>>> ReadComplianceDocumentMetadataAsync(
        Guid exportRequestId,
        CancellationToken cancellationToken = default)
    {
        if (exportRequestId == Guid.Empty)
        {
            return new AgentToolResult<IReadOnlyList<ComplianceAgentDocumentDto>>
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Export request ID is invalid."
            };
        }

        var requestExists = await _context.ExportRequests
            .AsNoTracking()
            .AnyAsync(r => r.Id == exportRequestId, cancellationToken);

        if (!requestExists)
        {
            return new AgentToolResult<IReadOnlyList<ComplianceAgentDocumentDto>>
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
            };
        }

        var documents = await _context.ComplianceDocuments
            .AsNoTracking()
            .Where(d => d.ExportRequestId == exportRequestId)
            .OrderBy(d => d.UploadedAt)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

        var mapped = documents.Select(d =>
        {
            var (effStatus, effReason) = Gemora.Domain.Helpers.ComplianceDocumentStatusHelper.CalculateEffectiveStatus(d);
            return new ComplianceAgentDocumentDto
            {
                DocumentId = d.Id,
                DocumentType = d.DocumentType,
                DocumentNumber = d.DocumentNumber,
                Issuer = d.Issuer,
                IssueDate = d.IssueDate,
                ExpiryDate = d.ExpiryDate,
                Status = d.Status.ToString(),
                EffectiveStatus = effStatus,
                EffectiveStatusReason = effReason,
                HasUploadedFile = !string.IsNullOrWhiteSpace(d.FileUrl)
            };
        }).ToList();

        return new AgentToolResult<IReadOnlyList<ComplianceAgentDocumentDto>>
        {
            Success = true,
            Message = "Compliance document metadata retrieved successfully.",
            Data = mapped
        };
    }

    // ======================================================
    // 3. RUN COMPLIANCE RULES VALIDATOR
    // ======================================================
    public async Task<AgentToolResult<ComplianceCheckResultDto>> RunComplianceRulesValidatorAsync(
        Guid exportRequestId,
        CancellationToken cancellationToken = default)
    {
        if (exportRequestId == Guid.Empty)
        {
            return new AgentToolResult<ComplianceCheckResultDto>
            {
                Success = false,
                ErrorCode = "INVALID_REQUEST",
                Message = "Export request ID is invalid."
            };
        }

        var request = await _context.ExportRequests
            .AsNoTracking()
            .Select(r => new { r.Id, r.RequestedByUserId })
            .FirstOrDefaultAsync(r => r.Id == exportRequestId, cancellationToken);

        if (request == null)
        {
            return new AgentToolResult<ComplianceCheckResultDto>
            {
                Success = false,
                ErrorCode = "REQUEST_NOT_FOUND",
                Message = "Export request was not found."
            };
        }

        var evalResult = await _complianceRulesService.EvaluateAsync(
            request.RequestedByUserId,
            exportRequestId);

        if (!evalResult.Success)
        {
            return new AgentToolResult<ComplianceCheckResultDto>
            {
                Success = false,
                ErrorCode = evalResult.ErrorCode ?? "EVALUATION_FAILED",
                Message = evalResult.Message
            };
        }

        return new AgentToolResult<ComplianceCheckResultDto>
        {
            Success = true,
            Message = evalResult.Message,
            Data = evalResult.Check
        };
    }
}
