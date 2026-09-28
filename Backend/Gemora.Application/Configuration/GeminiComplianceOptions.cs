namespace Gemora.Application.Configuration;

public class GeminiComplianceOptions
{
    public const string SectionName = "GeminiCompliance";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.6-flash";

    public string ThinkingLevel { get; set; } = "high";

    public int TimeoutSeconds { get; set; } = 45;

    public int MaxRetries { get; set; } = 1;
}
