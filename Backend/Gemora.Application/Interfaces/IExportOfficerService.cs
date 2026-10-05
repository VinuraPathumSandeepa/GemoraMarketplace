using Gemora.Application.DTOs.ExportCompliance;

namespace Gemora.Application.Interfaces;

public interface IExportOfficerService
{
    Task<ExportOfficerOperationResult> GetReviewQueueAsync(
        Guid officerUserId);

    Task<ExportOfficerOperationResult> GetRequestForReviewAsync(
        Guid officerUserId,
        Guid exportRequestId);

    Task<ExportOfficerOperationResult> StartReviewAsync(
        Guid officerUserId,
        Guid exportRequestId);

    Task<ExportOfficerOperationResult> MakeDecisionAsync(
        Guid officerUserId,
        Guid exportRequestId,
        ExportDecisionDto dto);

    Task<ComplianceDocumentFileResult> GetDocumentFileForReviewAsync(
        Guid officerUserId,
        Guid exportRequestId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}

