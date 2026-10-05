using Gemora.Application.DTOs.ExportCompliance;

namespace Gemora.Application.Interfaces;

public interface IComplianceRulesService
{
    Task<ComplianceEvaluationResult> EvaluateAsync(
        Guid userId,
        Guid exportRequestId);
}
