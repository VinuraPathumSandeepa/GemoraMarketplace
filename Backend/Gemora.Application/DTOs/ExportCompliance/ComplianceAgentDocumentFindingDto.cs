namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceAgentDocumentFindingDto
{
    public Guid DocumentId { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string Finding { get; set; } = string.Empty;

    public string Severity { get; set; } = "Info";
}
