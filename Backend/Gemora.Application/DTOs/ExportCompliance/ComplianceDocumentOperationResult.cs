namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceDocumentOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public ComplianceDocumentResponseDto? Document { get; set; }

    public IReadOnlyList<ComplianceDocumentResponseDto>? Documents { get; set; }
}
