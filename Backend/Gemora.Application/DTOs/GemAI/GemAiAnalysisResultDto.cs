namespace Gemora.Application.DTOs.GemAI;

public class GemAiAnalysisResultDto
{
    public string Status { get; set; } = string.Empty;

    public string? SuggestedGemType { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string Findings { get; set; } = string.Empty;

    public bool ImageAnalyzed { get; set; }

    public List<string> VisualObservations { get; set; } = new();

    public List<string> EvidenceWarnings { get; set; } = new();

    public List<string> RiskFlags { get; set; } = new();

    public List<string> ValidationIssues { get; set; } = new();

    public List<string> StepsCompleted { get; set; } = new();

    public List<GemAiWorkflowStepDto> WorkflowSteps { get; set; } = new();

    public string? ErrorCategory { get; set; }

    public int? RetryAfterSeconds { get; set; }
}

public class GemAiWorkflowStepDto
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";
}
