using Gemora.Domain.Entities;

namespace Gemora.Domain.AI;

public interface IGemAiModelClient
{
    Task<GemAiModelResult> AnalyzeAsync(
        GemListing listing,
        Stream? imageStream = null,
        string? imageContentType = null,
        CancellationToken cancellationToken = default);
}