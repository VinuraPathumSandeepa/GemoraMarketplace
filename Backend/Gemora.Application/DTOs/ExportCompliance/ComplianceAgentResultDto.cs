namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceAgentResultDto
{
    public string Summary { get; set; } = string.Empty;

    public bool DeterministicComplete { get; set; }

    public List<string> MissingRequirements { get; set; } = new();

    public List<ComplianceAgentDocumentFindingDto> DocumentFindings { get; set; } = new();

    public List<string> Inconsistencies { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<string> RecommendedOfficerChecks { get; set; } = new();

    public bool RequiresOfficerAttention { get; set; }

    public double Confidence { get; set; }

    public string Disclaimer { get; set; } = string.Empty;
}
