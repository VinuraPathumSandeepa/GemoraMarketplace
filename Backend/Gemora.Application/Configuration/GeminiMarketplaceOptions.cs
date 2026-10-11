namespace Gemora.Application.Configuration;

public class GeminiMarketplaceOptions
{
    public const string SectionName =
        "GeminiMarketplace";


    public string ApiKey
        { get; set; } = string.Empty;


    public string Model
        { get; set; } =
        "gemini-3.8-flash";


    public string BaseUrl
        { get; set; } =
        "https://generativelanguage.googleapis.com/";
}