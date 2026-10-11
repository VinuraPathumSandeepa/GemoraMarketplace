namespace Gemora.Application.DTOs.ExportCompliance;

public class ComplianceCheckResultDto
{
    public bool IsComplete { get; set; }

    public List<string> MissingRequirements { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<string> InvalidDocuments { get; set; } = new();
}
