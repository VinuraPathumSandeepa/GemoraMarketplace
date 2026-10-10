namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceEvaluationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public ComplianceCheckResultDto? Check { get; set; }
}
