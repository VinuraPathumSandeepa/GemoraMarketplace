namespace Gemora.Application.DTOs.GemAI;

public class GemAiAnalysisResultDto
{
    // Overall execution status:
    // Completed, Failed, NeedsMoreEvidence
    public string Status { get; set; } = string.Empty;

    // AI's suggested gemstone type based on the available evidence.
    // This is a suggestion only — not an authentication decision.
    public string? SuggestedGemType { get; set; }

    // 0.00 - 1.00
    // Represents confidence in the AI's own analysis.
    public decimal? ConfidenceScore { get; set; }

    // Human-readable explanation for the Gemologist.
    public string Findings { get; set; } = string.Empty;

    // Structured risk flags detected during analysis.
    public List<string> RiskFlags { get; set; } = new();

    // Deterministic validation issues found before/alongside AI analysis.
    public List<string> ValidationIssues { get; set; } = new();

    // Records which steps were performed.
    // This helps us demonstrate the multi-step agent workflow.
    public List<string> StepsCompleted { get; set; } = new();
}