namespace Gemora.Application.DTOs.MarketplaceAgent;

public class MarketplaceAgentRequestDto
{
    public string Message { get; set; } =
        string.Empty;


    // Optional conversation identifier.
    // Later frontend chat history ekata use karanna puluwan.
    public string? ConversationId
        { get; set; }
}