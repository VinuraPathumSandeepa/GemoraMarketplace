namespace Gemora.Application.DTOs.MarketplaceAgent;

public class MarketplaceAgentToolResultDto
{
    public string ToolName { get; set; } =
        string.Empty;


    public bool Success { get; set; }


    public string? Message { get; set; }


    public object? Data { get; set; }
}