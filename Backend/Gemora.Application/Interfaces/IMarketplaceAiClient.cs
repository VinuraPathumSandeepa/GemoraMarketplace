using Gemora.Application.DTOs.MarketplaceAgent;

namespace Gemora.Application.Interfaces;

public interface IMarketplaceAiClient
{
    Task<MarketplaceAiDecisionDto> GetDecisionAsync(
        string userMessage,
        string systemPrompt,
        CancellationToken cancellationToken = default
    );


    Task<string> GenerateFinalResponseAsync(
        string userMessage,
        string systemPrompt,
        MarketplaceAgentToolResultDto toolResult,
        CancellationToken cancellationToken = default
    );
}