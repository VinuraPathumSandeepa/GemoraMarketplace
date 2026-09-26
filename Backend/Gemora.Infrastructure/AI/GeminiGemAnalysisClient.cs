using System.Net.Http.Json;
using System.Text.Json;
using Gemora.Domain.AI;
using Gemora.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Gemora.Infrastructure.AI;

public class GeminiGemAnalysisClient : IGemAiModelClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public GeminiGemAnalysisClient(
        HttpClient httpClient,
        IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }


    public async Task<GemAiModelResult> AnalyzeAsync(
        GemListing listing,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "Gemini model is not configured.");
        }


        // ========================================================
        // BUILD CONTROLLED PROMPT
        // ========================================================

        var prompt =
            BuildPrompt(listing);


        // ========================================================
        // GEMINI REQUEST BODY
        // ========================================================

        var requestBody =
            new
            {
                contents =
                    new[]
                    {
                        new
                        {
                            parts =
                                new[]
                                {
                                    new
                                    {
                                        text = prompt
                                    }
                                }
                        }
                    },

                generationConfig =
                    new
                    {
                        temperature = 0.2,

                        responseMimeType =
                            "application/json"
                    }
            };


        // ========================================================
        // GEMINI ENDPOINT
        // ========================================================

        var endpoint =
            $"v1beta/models/{_options.Model}:generateContent" +
            $"?key={Uri.EscapeDataString(_options.ApiKey)}";


        // ========================================================
        // SEND REQUEST
        // ========================================================

        using var response =
            await _httpClient.PostAsJsonAsync(
                endpoint,
                requestBody,
                cancellationToken);


        var responseText =
            await response.Content.ReadAsStringAsync(
                cancellationToken);


        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Gemini analysis request failed with HTTP {(int)response.StatusCode}.");
        }


        // ========================================================
        // PARSE GEMINI RESPONSE
        // ========================================================

        using var responseDocument =
            JsonDocument.Parse(
                responseText);


        if (!responseDocument.RootElement
                .TryGetProperty(
                    "candidates",
                    out var candidates) ||
            candidates.ValueKind !=
                JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Gemini returned no analysis candidate.");
        }


        var firstCandidate =
            candidates[0];


        if (!firstCandidate.TryGetProperty(
                "content",
                out var content))
        {
            throw new InvalidOperationException(
                "Gemini returned an invalid content response.");
        }


        if (!content.TryGetProperty(
                "parts",
                out var parts) ||
            parts.ValueKind !=
                JsonValueKind.Array ||
            parts.GetArrayLength() == 0)
        {
            throw new InvalidOperationException(
                "Gemini returned no analysis content.");
        }


        if (!parts[0].TryGetProperty(
                "text",
                out var textElement))
        {
            throw new InvalidOperationException(
                "Gemini analysis response did not contain text.");
        }


        var modelJson =
            textElement.GetString();


        if (string.IsNullOrWhiteSpace(
                modelJson))
        {
            throw new InvalidOperationException(
                "Gemini returned an empty analysis.");
        }


        // ========================================================
        // DESERIALIZE STRUCTURED RESULT
        // ========================================================

        GeminiStructuredResponse?
            structuredResponse;

        try
        {
            structuredResponse =
                JsonSerializer.Deserialize<
                    GeminiStructuredResponse>(
                    modelJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive =
                            true
                    });
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(
                "Gemini returned an invalid structured analysis response.");
        }


        if (structuredResponse == null)
        {
            throw new InvalidOperationException(
                "Gemini returned an empty structured analysis response.");
        }


        // ========================================================
        // DEFENSIVE CONFIDENCE CHECK
        // ========================================================

        decimal? confidence =
            structuredResponse
                .ConfidenceScore;


        if (confidence.HasValue)
        {
            confidence =
                Math.Clamp(
                    confidence.Value,
                    0m,
                    1m);
        }


        // ========================================================
        // RETURN PROVIDER-INDEPENDENT DOMAIN RESULT
        // ========================================================

        return new GemAiModelResult
        {
            SuggestedGemType =
                structuredResponse
                    .SuggestedGemType,

            ConfidenceScore =
                confidence,

            Findings =
                string.IsNullOrWhiteSpace(
                    structuredResponse.Findings)
                    ? "AI analysis completed without detailed findings."
                    : structuredResponse.Findings,

            RiskFlags =
                structuredResponse.RiskFlags
                ?? new List<string>()
        };
    }


    // ============================================================
    // CONTROLLED GEM ANALYSIS PROMPT
    // ============================================================

    private static string BuildPrompt(
        GemListing listing)
    {
        return $$"""
        You are the AI-assisted evidence analysis agent for the
        Gemora gemstone marketplace.

        Your role is advisory.

        You MUST NOT claim that a gemstone is definitively authentic,
        certified, natural, untreated, or of a particular origin.

        A qualified human Gemologist makes the final verification
        decision.

        Analyze the listing information below for consistency,
        completeness, and potential risk indicators.

        DECLARED LISTING INFORMATION

        Title: {{listing.Title}}
        Declared Gem Type: {{listing.GemType}}
        Carat Weight: {{listing.CaratWeight}}
        Color: {{listing.Color}}
        Clarity: {{listing.Clarity}}
        Cut: {{listing.Cut}}
        Price: {{listing.Price}}
        Currency: {{listing.Currency}}
        Certificate Number: {{listing.CertificateNumber}}
        Certificate Authority: {{listing.CertificateAuthority}}

        IMPORTANT:

        At this stage you are analyzing only the supplied listing
        metadata.

        Do not claim that you visually inspected the gemstone image.

        Do not claim that you independently verified or read the
        uploaded certificate.

        Look for:

        - inconsistent gemstone characteristics
        - incomplete or unusual metadata
        - certificate-related risk indicators
        - information requiring human verification

        Return ONLY valid JSON.

        Use exactly this structure:

        {
          "suggestedGemType": "string or null",
          "confidenceScore": 0.0,
          "findings": "short factual advisory explanation",
          "riskFlags": [
            "risk flag"
          ]
        }

        confidenceScore must be between 0.0 and 1.0.

        The confidence score represents confidence in the consistency
        of the advisory classification.

        It DOES NOT represent confidence that the gemstone is
        authentic.
        """;
    }


    // ============================================================
    // GEMINI-SPECIFIC RESPONSE MODEL
    //
    // Kept private because the rest of Gemora should not depend
    // on Gemini's response representation.
    // ============================================================

    private sealed class GeminiStructuredResponse
    {
        public string? SuggestedGemType
        {
            get;
            set;
        }

        public decimal? ConfidenceScore
        {
            get;
            set;
        }

        public string? Findings
        {
            get;
            set;
        }

        public List<string>? RiskFlags
        {
            get;
            set;
        }
    }
}