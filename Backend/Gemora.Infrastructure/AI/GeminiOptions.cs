
namespace Gemora.Infrastructure.AI;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    // Gemini API Key
    public string ApiKey { get; set; } = string.Empty;

    // Default Gemini model
    public string Model { get; set; } = "gemini-3.5-flash-lite";

    // Optional separate model for shipping agent
    // Falls back to Model when not configured
    public string ShippingModel { get; set; } = string.Empty;

    // Total timeout for shipping agent requests
    public int ShippingTimeoutSeconds { get; set; } = 120;
}
