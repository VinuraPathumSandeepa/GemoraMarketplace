namespace Gemora.Domain.AI;

public class GemAiModelResult
{
    public string? SuggestedGemType { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string Findings { get; set; } = string.Empty;

    public List<string> RiskFlags { get; set; } = new();
}