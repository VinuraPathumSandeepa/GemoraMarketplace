namespace Gemora.Application.DTOs.MarketplaceAgent;

public class MarketplaceAgentResponseDto
{
    public string Message { get; set; } =
        string.Empty;


    public string? ConversationId
        { get; set; }


    public string Model { get; set; } =
        string.Empty;


    public List<string> ToolsUsed
        { get; set; } = new();


    public DateTime GeneratedAt
        { get; set; } =
        DateTime.UtcNow;
}