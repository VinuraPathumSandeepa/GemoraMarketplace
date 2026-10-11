using Gemora.Application.DTOs.Agent;
using Gemora.Application.DTOs.ExportCompliance;

namespace Gemora.Application.Interfaces;

public interface IComplianceAgentToolService
{
    Task<AgentToolResult<ComplianceAgentExportRequestDto>> ReadExportRequestAsync(
        Guid exportRequestId,
        CancellationToken cancellationToken = default);

    Task<AgentToolResult<IReadOnlyList<ComplianceAgentDocumentDto>>> ReadComplianceDocumentMetadataAsync(
        Guid exportRequestId,
        CancellationToken cancellationToken = default);

    Task<AgentToolResult<ComplianceCheckResultDto>> RunComplianceRulesValidatorAsync(
        Guid exportRequestId,
        CancellationToken cancellationToken = default);
}
