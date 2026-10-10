namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceAgentContextDto
{
    public ComplianceAgentExportRequestDto ExportRequest { get; set; } = new();

    public IReadOnlyList<ComplianceAgentDocumentDto> Documents { get; set; }
        = Array.Empty<ComplianceAgentDocumentDto>();

    public ComplianceCheckResultDto DeterministicCheck { get; set; } = new();
}
