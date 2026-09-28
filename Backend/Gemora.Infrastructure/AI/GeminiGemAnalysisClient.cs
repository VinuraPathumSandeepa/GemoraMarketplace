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
        Stream? imageStream = null,
        string? imageContentType = null,
        CancellationToken cancellationToken = default)
    {
        // ========================================================
        // STEP 1 — CONFIGURATION VALIDATION
        // ========================================================

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
        // STEP 2 — CHECK WHETHER AN IMAGE IS AVAILABLE
        // ========================================================

        var hasImage =
            imageStream != null &&
            !string.IsNullOrWhiteSpace(imageContentType);


        // ========================================================
        // STEP 3 — BUILD CONTROLLED PROMPT
        // ========================================================

        var prompt =
            BuildPrompt(
                listing,
                hasImage);


        // ========================================================
        // STEP 4 — BUILD GEMINI CONTENT PARTS
        //
        // Gemini multimodal request:
        //
        // parts
        // ├── text
        // └── inlineData
        //      ├── mimeType
        //      └── Base64 image
        // ========================================================

        var parts =
            new List<object>
            {
                new
                {
                    text = prompt
                }
            };


        if (hasImage)
        {
            var base64Image =
                await ConvertStreamToBase64Async(
                    imageStream!,
                    cancellationToken);


            parts.Add(
                new
                {
                    inlineData =
                        new
                        {
                            mimeType =
                                imageContentType,

                            data =
                                base64Image
                        }
                });
        }


        // ========================================================
        // STEP 5 — BUILD REQUEST BODY
        // ========================================================

        var requestBody =
            new
            {
                contents =
                    new[]
                    {
                        new
                        {
                            role = "user",

                            parts =
                                parts.ToArray()
                        }
                    },

                generationConfig =
                    new
                    {
                        // Low temperature keeps the analysis
                        // more consistent and less creative.
                        temperature = 0.2,

                        responseMimeType =
                            "application/json"
                    }
            };


        // ========================================================
        // STEP 6 — BUILD GEMINI ENDPOINT
        // ========================================================

        var endpoint =
            $"v1beta/models/{_options.Model}:generateContent" +
            $"?key={Uri.EscapeDataString(_options.ApiKey)}";


        // ========================================================
        // STEP 7 — CALL GEMINI WITH TRANSIENT-ERROR RETRY
        //
        // Retry:
        //
        // 408       Request timeout
        // 429       Rate limit / resource exhaustion
        // 500–599   Temporary provider/server failures
        //
        // Backoff:
        //
        // Attempt 1 fails → wait 1 second
        // Attempt 2 fails → wait 2 seconds
        // Attempt 3 fails → wait 4 seconds
        // Attempt 4 fails → stop
        // ========================================================

        HttpResponseMessage? response =
            null;


        string responseText =
            string.Empty;


        const int maxAttempts =
            4;


        try
        {
            for (var attempt = 1;
                 attempt <= maxAttempts;
                 attempt++)
            {
                // Dispose any response from the previous attempt.
                response?.Dispose();


                response =
                    await _httpClient.PostAsJsonAsync(
                        endpoint,
                        requestBody,
                        cancellationToken);


                responseText =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);


                // =================================================
                // SUCCESS
                // =================================================

                if (response.IsSuccessStatusCode)
                {
                    break;
                }


                var statusCode =
                    (int)response.StatusCode;


                // =================================================
                // DETERMINE WHETHER FAILURE IS TRANSIENT
                // =================================================

                var transientError =
                    statusCode == 408 ||
                    statusCode == 429 ||
                    statusCode >= 500;


                // =================================================
                // DO NOT RETRY PERMANENT ERRORS
                //
                // Examples:
                //
                // 400 bad request
                // 401 authentication
                // 403 permission
                // 404 invalid/unavailable model
                // =================================================

                if (!transientError)
                {
                    throw new InvalidOperationException(
                        $"Gemini analysis request failed with HTTP {statusCode}.");
                }


                // =================================================
                // LAST ATTEMPT FAILED
                // =================================================

                if (attempt == maxAttempts)
                {
                    throw new InvalidOperationException(
                        $"Gemini analysis request failed with HTTP {statusCode} after {maxAttempts} attempts.");
                }


                // =================================================
                // EXPONENTIAL BACKOFF
                //
                // attempt 1 → 1 second
                // attempt 2 → 2 seconds
                // attempt 3 → 4 seconds
                // =================================================

                var delaySeconds =
                    Math.Pow(
                        2,
                        attempt - 1);


                await Task.Delay(
                    TimeSpan.FromSeconds(
                        delaySeconds),
                    cancellationToken);
            }


            // ====================================================
            // STEP 8 — FINAL RESPONSE SAFETY CHECK
            // ====================================================

            if (response == null ||
                !response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    "Gemini analysis request failed after multiple attempts.");
            }


            // ====================================================
            // STEP 9 — PARSE GEMINI RESPONSE
            // ====================================================

            JsonDocument responseDocument;

            try
            {
                responseDocument =
                    JsonDocument.Parse(
                        responseText);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(
                    "Gemini returned an invalid JSON response.");
            }


            using (responseDocument)
            {
                // =================================================
                // GET CANDIDATES
                // =================================================

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


                // =================================================
                // GET CONTENT
                // =================================================

                if (!firstCandidate.TryGetProperty(
                        "content",
                        out var content))
                {
                    throw new InvalidOperationException(
                        "Gemini returned an invalid content response.");
                }


                // =================================================
                // GET RESPONSE PARTS
                // =================================================

                if (!content.TryGetProperty(
                        "parts",
                        out var responseParts) ||
                    responseParts.ValueKind !=
                        JsonValueKind.Array ||
                    responseParts.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException(
                        "Gemini returned no analysis content.");
                }


                // =================================================
                // GET STRUCTURED JSON TEXT
                // =================================================

                if (!responseParts[0].TryGetProperty(
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


                // =================================================
                // STEP 10 — DESERIALIZE STRUCTURED AI RESULT
                // =================================================

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


                // =================================================
                // STEP 11 — DEFENSIVE CONFIDENCE VALIDATION
                // =================================================

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


                // =================================================
                // STEP 12 — RETURN PROVIDER-INDEPENDENT RESULT
                // =================================================

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


                    VisualObservations =
                        structuredResponse
                            .VisualObservations
                        ?? new List<string>(),


                    RiskFlags =
                        structuredResponse
                            .RiskFlags
                        ?? new List<string>(),


                    // IMPORTANT:
                    //
                    // This becomes true only when an actual image
                    // stream was supplied in the Gemini request.
                    ImageAnalyzed =
                        hasImage
                };
            }
        }
        finally
        {
            response?.Dispose();
        }
    }


    // ============================================================
    // IMAGE STREAM → BASE64
    //
    // Image remains in memory.
    // No temporary AI file is created.
    // ============================================================

    private static async Task<string>
        ConvertStreamToBase64Async(
            Stream stream,
            CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position =
                0;
        }


        using var memoryStream =
            new MemoryStream();


        await stream.CopyToAsync(
            memoryStream,
            cancellationToken);


        return Convert.ToBase64String(
            memoryStream.ToArray());
    }


    // ============================================================
    // CONTROLLED MULTIMODAL PROMPT
    // ============================================================

    private static string BuildPrompt(
        GemListing listing,
        bool hasImage)
    {
        var imageInstruction =
            hasImage
                ? """
                  A gemstone photograph has been supplied with this
                  request.

                  Inspect the photograph only for visible
                  characteristics.

                  You may describe:

                  - apparent colour
                  - apparent shape
                  - visible cut style
                  - apparent transparency
                  - visible inclusions or surface features
                  - obvious visual inconsistencies with the seller's
                    declared information

                  IMPORTANT LIMITATIONS:

                  A photograph alone cannot establish:

                  - gemstone authenticity
                  - whether the stone is natural or synthetic
                  - treatment status
                  - geographic origin
                  - laboratory certification
                  - monetary value

                  Never claim that the photograph proves authenticity.
                  """
                : """
                  No gemstone photograph has been supplied to the AI
                  model.

                  Do not make visual observations.

                  Do not imply that you inspected a gemstone image.
                  """;


        return $$"""
        You are the AI-assisted gemstone evidence analysis agent for
        the Gemora marketplace.

        Your role is advisory.

        A qualified human Gemologist makes the final verification
        decision.

        You MUST NOT make the final marketplace approval or rejection
        decision.

        ============================================================
        DECLARED LISTING INFORMATION
        ============================================================

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


        ============================================================
        IMAGE ANALYSIS
        ============================================================

        {{imageInstruction}}


        ============================================================
        CERTIFICATE LIMITATION
        ============================================================

        Certificate metadata may be included in the listing
        information.

        Unless the actual certificate document contents have
        separately been supplied to you, do NOT claim that you:

        - read the certificate
        - validated the certificate
        - authenticated the certificate
        - contacted the certificate authority
        - confirmed the certificate number


        ============================================================
        ANALYSIS TASKS
        ============================================================

        1. Examine the declared gemstone information.

        2. Check the metadata for internal consistency.

        3. If a photograph was supplied, examine only visible
           characteristics.

        4. Compare visible characteristics with the seller's
           declared gemstone information.

        5. Identify obvious inconsistencies.

        6. Identify missing information.

        7. Identify unusual or suspicious claims.

        8. Identify evidence requiring human Gemologist review.

        9. Suggest a possible gemstone type only when reasonable.

        10. Keep all findings factual and advisory.


        ============================================================
        OUTPUT
        ============================================================

        Return ONLY valid JSON.

        Do not return Markdown.

        Do not return ```json code fences.

        Use exactly this structure:

        {
          "suggestedGemType": "string or null",
          "confidenceScore": 0.0,
          "findings": "short factual advisory explanation",
          "visualObservations": [
            "observation"
          ],
          "riskFlags": [
            "risk flag"
          ]
        }


        ============================================================
        CONFIDENCE SCORE
        ============================================================

        confidenceScore must be between:

        0.0 and 1.0

        The confidence score represents confidence in the consistency
        of the advisory classification.

        It does NOT represent confidence that the gemstone is
        authentic.


        ============================================================
        HUMAN APPROVAL
        ============================================================

        Your result is advisory evidence for a human Gemologist.

        Never state that the listing should automatically be approved
        or rejected.
        """;
    }


    // ============================================================
    // GEMINI-SPECIFIC STRUCTURED RESPONSE
    //
    // This remains private so the rest of Gemora does not depend
    // directly on Gemini's output representation.
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


        public List<string>? VisualObservations
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