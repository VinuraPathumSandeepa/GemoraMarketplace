using Gemora.Application.DTOs.ExportCompliance;

namespace Gemora.Application.Interfaces;

public interface IExportComplianceService
{
    Task<ExportRequestOperationResult> CreateExportRequestAsync(
        Guid userId,
        CreateExportRequestDto dto);

    Task<ExportRequestOperationResult> GetMyExportRequestsAsync(
        Guid userId);

    Task<ExportRequestOperationResult> GetExportRequestByIdAsync(
        Guid userId,
        Guid exportRequestId);

    Task<ExportRequestOperationResult> UpdateExportRequestAsync(
        Guid userId,
        Guid exportRequestId,
        UpdateExportRequestDto dto);

    Task<ExportRequestOperationResult> SubmitExportRequestAsync(
        Guid userId,
        Guid exportRequestId);

    Task<ComplianceDocumentOperationResult> AddComplianceDocumentAsync(
        Guid userId,
        Guid exportRequestId,
        CreateComplianceDocumentDto dto);

    Task<ComplianceDocumentOperationResult> GetComplianceDocumentsAsync(
        Guid userId,
        Guid exportRequestId);
}
