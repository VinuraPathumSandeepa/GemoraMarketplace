namespace Gemora.Infrastructure.AI;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-3.5-flash-lite";

    // Optional override for shipping; other Gemini features continue using Model.
    public string? ShippingModel { get; set; }
    public int ShippingTimeoutSeconds { get; set; } = 120;
}
