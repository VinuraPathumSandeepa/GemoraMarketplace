using Gemora.Application.DTOs.ExportCompliance;

namespace Gemora.Application.Interfaces;

public interface IComplianceWorkflowService
{
    Task<ComplianceWorkflowExecutionResult> StartContextCollectionAsync(
        Guid triggeredByUserId,
        Guid exportRequestId,
        CancellationToken cancellationToken = default);
}
