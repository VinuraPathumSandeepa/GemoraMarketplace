namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceDocumentFileResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }

    public Stream? Content { get; set; }

    public string? ContentType { get; set; }

    public string? DownloadFileName { get; set; }
}
