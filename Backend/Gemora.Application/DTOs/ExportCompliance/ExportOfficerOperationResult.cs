namespace Gemora.Application.DTOs.ExportCompliance;

public class ExportOfficerOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public OfficerExportRequestResponseDto? Request { get; set; }

    public IReadOnlyList<OfficerExportRequestResponseDto>? Requests { get; set; }
}
