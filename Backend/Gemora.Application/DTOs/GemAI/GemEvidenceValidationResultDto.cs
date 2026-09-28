namespace Gemora.Application.DTOs.GemAI;

public class GemEvidenceValidationResultDto
{
    public bool IsValid { get; set; }

    public List<string> Issues { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<string> ChecksPerformed { get; set; } = new();
}