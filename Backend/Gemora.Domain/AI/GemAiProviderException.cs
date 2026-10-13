namespace Gemora.Domain.AI;

public sealed class GemAiProviderException : Exception
{
    public string Category { get; }
    public int? RetryAfterSeconds { get; }

    public GemAiProviderException(string category, int? retryAfterSeconds = null)
        : base($"Gemini analysis failed: {category}.")
    {
        Category = category;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
