using Gemora.Application.DTOs.ExportCompliance;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Enums;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ComplianceRulesService : IComplianceRulesService
{
    private readonly ApplicationDbContext _context;

    public ComplianceRulesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ComplianceEvaluationResult> EvaluateAsync(
        Guid userId,
        Guid exportRequestId)
    {
        // 1. Validate authenticated user ID
        if (userId == Guid.Empty)
        {
            return new ComplianceEvaluationResult
            {
                Success = false,
                Message = "Authenticated user is invalid.",
                ErrorCode = "INVALID_USER"
            };
        }

        // 2. Validate export request ID
        if (exportRequestId == Guid.Empty)
        {
            return new ComplianceEvaluationResult
            {
                Success = false,
                Message = "Export request ID is invalid.",
                ErrorCode = "INVALID_REQUEST"
            };
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return new ComplianceEvaluationResult
            {
                Success = false,
                Message = "Authenticated user was not found.",
                ErrorCode = "USER_NOT_FOUND"
            };
        }

        if (!string.Equals(user.Role, UserRoles.Seller, StringComparison.OrdinalIgnoreCase))
        {
            return new ComplianceEvaluationResult
            {
                Success = false,
                Message = "Only sellers can evaluate compliance.",
                ErrorCode = "FORBIDDEN"
            };
        }

        // 3. Retrieve owned export request
        var exportRequest = await _context.ExportRequests
            .AsNoTracking()
            .Include(r => r.ComplianceDocuments)
            .FirstOrDefaultAsync(r => r.Id == exportRequestId && r.RequestedByUserId == userId);

        if (exportRequest == null)
        {
            return new ComplianceEvaluationResult
            {
                Success = false,
                Message = "Export request was not found.",
                ErrorCode = "REQUEST_NOT_FOUND"
            };
        }

        // 4. Create deterministic compliance result
        var check = new ComplianceCheckResultDto();

        // 5. Export request field rules
        if (string.IsNullOrWhiteSpace(exportRequest.OriginCountry))
        {
            check.MissingRequirements.Add("Origin country is required.");
        }

        if (string.IsNullOrWhiteSpace(exportRequest.DestinationCountry))
        {
            check.MissingRequirements.Add("Destination country is required.");
        }

        if (exportRequest.DeclaredValue <= 0)
        {
            check.MissingRequirements.Add("Declared value must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(exportRequest.Currency))
        {
            check.MissingRequirements.Add("Currency is required.");
        }

        // 6. Document existence rule
        if (exportRequest.ComplianceDocuments == null || exportRequest.ComplianceDocuments.Count == 0)
        {
            check.MissingRequirements.Add("At least one compliance document is required.");
        }
        else
        {
            var todayUtc = DateTime.UtcNow.Date;

            // 7. Per-document deterministic checks
            foreach (var document in exportRequest.ComplianceDocuments)
            {
                // A. Document Type Allow-list Check
                if (string.IsNullOrWhiteSpace(document.DocumentType) ||
                    !ComplianceConstants.SupportedDocumentTypes.Any(t => string.Equals(t, document.DocumentType.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    check.InvalidDocuments.Add("Unsupported compliance document type.");
                }

                // B. Issuer Check
                if (string.IsNullOrWhiteSpace(document.Issuer))
                {
                    check.InvalidDocuments.Add("Certificate issuer is required.");
                }

                // C. Document Number Check
                if (string.IsNullOrWhiteSpace(document.DocumentNumber))
                {
                    check.InvalidDocuments.Add("Certificate document number is required.");
                }

                // D. Invalid date sequence
                if (document.IssueDate.HasValue && document.ExpiryDate.HasValue && document.ExpiryDate.Value < document.IssueDate.Value)
                {
                    check.InvalidDocuments.Add("Certificate expiry date cannot be earlier than issue date.");
                }

                // E. Expired document
                if (document.ExpiryDate.HasValue && document.ExpiryDate.Value.Date < todayUtc)
                {
                    check.InvalidDocuments.Add($"Certificate expired on {document.ExpiryDate.Value:dd/MM/yyyy}.");
                }

                // F. FileUrl warning (non-blocking)
                if (string.IsNullOrWhiteSpace(document.FileUrl))
                {
                    check.Warnings.Add($"Document {document.Id} does not yet have an uploaded file.");
                }
            }
        }

        // 8. Workflow-status warnings (non-blocking)
        if (exportRequest.Status == ExportRequestStatus.Draft)
        {
            check.Warnings.Add("Export request is still in Draft status.");
        }
        else if (exportRequest.Status == ExportRequestStatus.RevisionRequired)
        {
            check.Warnings.Add("Export request requires revision before continuing.");
        }

        // 9. Calculate IsComplete
        check.IsComplete =
            check.MissingRequirements.Count == 0 &&
            check.InvalidDocuments.Count == 0;

        // 10. Return successful evaluation
        return new ComplianceEvaluationResult
        {
            Success = true,
            Message = check.IsComplete
                ? "Compliance check completed successfully. No blocking issues were found."
                : "Compliance check completed. Blocking issues were found.",
            Check = check
        };
    }
}
