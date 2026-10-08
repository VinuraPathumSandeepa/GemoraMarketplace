using System.Text.Json;

namespace Gemora.Application.DTOs.MarketplaceAgent;

public class MarketplaceAiDecisionDto
{
    public bool RequiresTool { get; set; }


    public string? ToolName { get; set; }


    public JsonElement? ToolArguments { get; set; }


    public string? DirectResponse { get; set; }
}