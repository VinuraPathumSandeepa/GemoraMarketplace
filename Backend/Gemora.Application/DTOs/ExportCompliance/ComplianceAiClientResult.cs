namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceAiClientResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public ComplianceAgentResultDto? Result { get; set; }

    public string? ModelName { get; set; }

    public long DurationMs { get; set; }
}
