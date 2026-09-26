using Gemora.Domain.Entities;

namespace Gemora.Domain.AI;

public interface IGemAiModelClient
{
    Task<GemAiModelResult> AnalyzeAsync(
        GemListing listing,
        CancellationToken cancellationToken = default);
}