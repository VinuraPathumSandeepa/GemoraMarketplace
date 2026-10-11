using Gemora.Application.DTOs.ExportCompliance;

namespace Gemora.Application.Interfaces;

public interface IComplianceAiClient
{
    Task<ComplianceAiClientResult> AnalyzeAsync(
        ComplianceAgentContextDto context,
        CancellationToken cancellationToken = default);
}
