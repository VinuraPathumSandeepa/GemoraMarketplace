using System.Net;
using System.Text;
using System.Text.Json;

using Gemora.Application.Configuration;
using Gemora.Application.DTOs.MarketplaceAgent;
using Gemora.Application.Interfaces;

using Microsoft.Extensions.Options;

namespace Gemora.Infrastructure.AI;

public class GeminiMarketplaceAiClient
    : IMarketplaceAiClient
{
    private const int MaxGeminiAttempts = 4;

    private readonly HttpClient _httpClient;
    private readonly GeminiMarketplaceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;


    public GeminiMarketplaceAiClient(
        HttpClient httpClient,
        IOptions<GeminiMarketplaceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        _jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,

                PropertyNameCaseInsensitive =
                    true
            };
    }


    // ============================================================
    // GEMINI TOOL DECISION
    // ============================================================

    public async Task<MarketplaceAiDecisionDto>
        GetDecisionAsync(
            string userMessage,
            string systemPrompt,
            CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();


        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return new MarketplaceAiDecisionDto
            {
                RequiresTool = false,

                DirectResponse =
                    "Please enter a marketplace question."
            };
        }


        var requestBody =
            new
            {
                systemInstruction =
                    new
                    {
                        parts =
                            new[]
                            {
                                new
                                {
                                    text = systemPrompt
                                }
                            }
                    },

                contents =
                    new[]
                    {
                        new
                        {
                            role = "user",

                            parts =
                                new[]
                                {
                                    new
                                    {
                                        text = userMessage
                                    }
                                }
                        }
                    },

                tools =
                    BuildMarketplaceTools(),

                generationConfig =
                    new
                    {
                        temperature = 0.2,

                        thinkingConfig =
                            new
                            {
                                thinkingLevel =
                                    "low"
                            }
                    }
            };


        using var response =
            await SendGeminiRequestAsync(
                requestBody,
                cancellationToken);


        var responseJson =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);


        if (!response.IsSuccessStatusCode)
        {
            throw BuildGeminiException(
                response.StatusCode,
                responseJson);
        }


        using var document =
            JsonDocument.Parse(
                responseJson);


        return ParseDecision(
            document.RootElement);
    }


    // ============================================================
    // FINAL RESPONSE AFTER TOOL EXECUTION
    // ============================================================

    public async Task<string>
        GenerateFinalResponseAsync(
            string userMessage,
            string systemPrompt,
            MarketplaceAgentToolResultDto toolResult,
            CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();


        var toolJson =
            JsonSerializer.Serialize(
                toolResult,
                _jsonOptions);


        var finalPrompt =
            $"""
            Original buyer message:
            {userMessage}

            Gemora backend tool result:
            {toolJson}

            Using only the verified Gemora backend tool result above,
            provide a clear and helpful answer to the buyer.

            Important rules:

            - Do not invent gemstone listings.
            - Do not invent order information.
            - Do not invent prices.
            - Do not invent shipment details.
            - Do not claim an action was completed unless the tool result confirms it.
            - If the tool failed, explain that the requested information could not be retrieved.
            - Keep monetary values in their real currency.
            - Clearly distinguish marketplace facts from general educational guidance.
            - Never expose internal database IDs unless they are useful to the buyer.
            - Never expose API keys, tokens, secrets, or backend implementation details.
            """;


        var requestBody =
            new
            {
                systemInstruction =
                    new
                    {
                        parts =
                            new[]
                            {
                                new
                                {
                                    text = systemPrompt
                                }
                            }
                    },

                contents =
                    new[]
                    {
                        new
                        {
                            role = "user",

                            parts =
                                new[]
                                {
                                    new
                                    {
                                        text = finalPrompt
                                    }
                                }
                        }
                    },

                generationConfig =
                    new
                    {
                        temperature = 0.3,

                        thinkingConfig =
                            new
                            {
                                thinkingLevel =
                                    "low"
                            }
                    }
            };


        using var response =
            await SendGeminiRequestAsync(
                requestBody,
                cancellationToken);


        var responseJson =
            await response.Content
                .ReadAsStringAsync(
                    cancellationToken);


        if (!response.IsSuccessStatusCode)
        {
            throw BuildGeminiException(
                response.StatusCode,
                responseJson);
        }


        using var document =
            JsonDocument.Parse(
                responseJson);


        var text =
            ExtractText(
                document.RootElement);


        if (string.IsNullOrWhiteSpace(text))
        {
            return
                "I retrieved the marketplace information, " +
                "but I was unable to generate a response.";
        }


        return text.Trim();
    }


    // ============================================================
    // SEND GEMINI REQUEST
    //
    // Automatic retry:
    //
    // Attempt 1 -> normal request
    // Retry 1   -> approximately 1 second
    // Retry 2   -> approximately 2 seconds
    // Retry 3   -> approximately 4 seconds
    //
    // Retry only:
    //
    // 408
    // 429
    // 5xx
    // ============================================================

    private async Task<HttpResponseMessage>
        SendGeminiRequestAsync(
            object requestBody,
            CancellationToken cancellationToken)
    {
        var model =
            Uri.EscapeDataString(
                _options.Model);


        var endpoint =
            $"v1beta/models/{model}:generateContent";


        var json =
            JsonSerializer.Serialize(
                requestBody,
                _jsonOptions);


        for (
            var attempt = 1;
            attempt <= MaxGeminiAttempts;
            attempt++
        )
        {
            cancellationToken
                .ThrowIfCancellationRequested();


            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    endpoint);


            request.Headers.Add(
                "x-goog-api-key",
                _options.ApiKey);


            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");


            try
            {
                var response =
                    await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption
                            .ResponseHeadersRead,
                        cancellationToken);


                // -----------------------------------------------
                // SUCCESS
                // -----------------------------------------------

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }


                // -----------------------------------------------
                // NON-RETRYABLE ERROR
                //
                // Examples:
                // 400 bad request
                // 401 invalid key
                // 403 permission
                // -----------------------------------------------

                if (!ShouldRetry(response.StatusCode))
                {
                    return response;
                }


                // -----------------------------------------------
                // LAST ATTEMPT
                // -----------------------------------------------

                if (attempt == MaxGeminiAttempts)
                {
                    return response;
                }


                // -----------------------------------------------
                // RETRY
                // -----------------------------------------------

                var delay =
                    GetRetryDelay(
                        response,
                        attempt);


                response.Dispose();


                await Task.Delay(
                    delay,
                    cancellationToken);
            }

            // ---------------------------------------------------
            // TEMPORARY NETWORK FAILURE
            // ---------------------------------------------------

            catch (HttpRequestException)
                when (
                    attempt <
                    MaxGeminiAttempts
                )
            {
                await Task.Delay(
                    GetFallbackRetryDelay(
                        attempt),
                    cancellationToken);
            }

            // ---------------------------------------------------
            // HTTP CLIENT TIMEOUT
            //
            // Do not retry when the actual caller explicitly
            // cancelled the request.
            // ---------------------------------------------------

            catch (TaskCanceledException)
                when (
                    !cancellationToken
                        .IsCancellationRequested
                    &&
                    attempt <
                    MaxGeminiAttempts
                )
            {
                await Task.Delay(
                    GetFallbackRetryDelay(
                        attempt),
                    cancellationToken);
            }
        }


        throw new HttpRequestException(
            "Gemini Marketplace AI request could not be completed.",
            null,
            HttpStatusCode.ServiceUnavailable
        );
    }


    // ============================================================
    // RETRYABLE STATUS
    // ============================================================

    private static bool ShouldRetry(
        HttpStatusCode statusCode)
    {
        var code =
            (int)statusCode;


        return
            statusCode ==
                HttpStatusCode.RequestTimeout
            ||
            code == 429
            ||
            code >= 500;
    }


    // ============================================================
    // RETRY DELAY
    //
    // Gemini Retry-After header is preferred when present.
    // ============================================================

    private static TimeSpan GetRetryDelay(
        HttpResponseMessage response,
        int attempt)
    {
        var retryAfter =
            response.Headers.RetryAfter;


        if (
            retryAfter?.Delta is
                TimeSpan retryDelta
            &&
            retryDelta >
                TimeSpan.Zero
        )
        {
            return LimitRetryDelay(
                retryDelta);
        }


        if (
            retryAfter?.Date is
                DateTimeOffset retryDate
        )
        {
            var serverDelay =
                retryDate -
                DateTimeOffset.UtcNow;


            if (
                serverDelay >
                    TimeSpan.Zero
            )
            {
                return LimitRetryDelay(
                    serverDelay);
            }
        }


        return GetFallbackRetryDelay(
            attempt);
    }


    // ============================================================
    // EXPONENTIAL BACKOFF + JITTER
    // ============================================================

    private static TimeSpan
        GetFallbackRetryDelay(
            int attempt)
    {
        var exponent =
            Math.Max(
                0,
                attempt - 1);


        var baseMilliseconds =
            1000d *
            Math.Pow(
                2,
                exponent);


        var jitterMilliseconds =
            Random.Shared.Next(
                150,
                650);


        var totalMilliseconds =
            Math.Min(
                8000d,
                baseMilliseconds +
                jitterMilliseconds);


        return TimeSpan.FromMilliseconds(
            totalMilliseconds);
    }


    private static TimeSpan LimitRetryDelay(
        TimeSpan delay)
    {
        var maximum =
            TimeSpan.FromSeconds(
                15);


        return delay >
               maximum
            ? maximum
            : delay;
    }


    // ============================================================
    // PARSE GEMINI DECISION
    // ============================================================

    private MarketplaceAiDecisionDto
        ParseDecision(
            JsonElement root)
    {
        if (
            !root.TryGetProperty(
                "candidates",
                out var candidates)
            ||
            candidates.ValueKind !=
                JsonValueKind.Array
            ||
            candidates.GetArrayLength() == 0
        )
        {
            return new MarketplaceAiDecisionDto
            {
                RequiresTool = false,

                DirectResponse =
                    "I could not generate a marketplace response."
            };
        }


        var candidate =
            candidates[0];


        if (
            !candidate.TryGetProperty(
                "content",
                out var content)
            ||
            !content.TryGetProperty(
                "parts",
                out var parts)
            ||
            parts.ValueKind !=
                JsonValueKind.Array
        )
        {
            return new MarketplaceAiDecisionDto
            {
                RequiresTool = false,

                DirectResponse =
                    "I could not understand the marketplace request."
            };
        }


        string? directText =
            null;


        foreach (
            var part in
            parts.EnumerateArray())
        {
            // ----------------------------------------------------
            // FUNCTION CALL
            // ----------------------------------------------------

            if (
                part.TryGetProperty(
                    "functionCall",
                    out var functionCall)
            )
            {
                var toolName =
                    functionCall.TryGetProperty(
                        "name",
                        out var nameElement)

                        ? nameElement.GetString()

                        : null;


                JsonElement? arguments =
                    null;


                if (
                    functionCall.TryGetProperty(
                        "args",
                        out var argsElement)
                )
                {
                    arguments =
                        argsElement.Clone();
                }


                return new
                    MarketplaceAiDecisionDto
                    {
                        RequiresTool = true,

                        ToolName =
                            toolName,

                        ToolArguments =
                            arguments
                    };
            }


            // ----------------------------------------------------
            // NORMAL TEXT
            // ----------------------------------------------------

            if (
                part.TryGetProperty(
                    "text",
                    out var textElement)
            )
            {
                var value =
                    textElement.GetString();


                if (
                    !string.IsNullOrWhiteSpace(
                        value)
                )
                {
                    directText =
                        value;
                }
            }
        }


        return new MarketplaceAiDecisionDto
        {
            RequiresTool = false,

            DirectResponse =
                directText ??
                "I could not generate a marketplace response."
        };
    }


    // ============================================================
    // EXTRACT NORMAL TEXT
    // ============================================================

    private static string?
        ExtractText(
            JsonElement root)
    {
        if (
            !root.TryGetProperty(
                "candidates",
                out var candidates)
            ||
            candidates.ValueKind !=
                JsonValueKind.Array
            ||
            candidates.GetArrayLength() == 0
        )
        {
            return null;
        }


        var candidate =
            candidates[0];


        if (
            !candidate.TryGetProperty(
                "content",
                out var content)
            ||
            !content.TryGetProperty(
                "parts",
                out var parts)
            ||
            parts.ValueKind !=
                JsonValueKind.Array
        )
        {
            return null;
        }


        var builder =
            new StringBuilder();


        foreach (
            var part in
            parts.EnumerateArray())
        {
            if (
                !part.TryGetProperty(
                    "text",
                    out var textElement)
            )
            {
                continue;
            }


            var text =
                textElement.GetString();


            if (
                string.IsNullOrWhiteSpace(
                    text)
            )
            {
                continue;
            }


            if (builder.Length > 0)
            {
                builder.AppendLine();
            }


            builder.Append(
                text);
        }


        return builder.ToString();
    }


    // ============================================================
    // TOOL DEFINITIONS
    // ============================================================

    private static object[]
        BuildMarketplaceTools()
    {
        return
        [
            new
            {
                functionDeclarations =
                    new object[]
                    {
                        // =========================================
                        // 1. SEARCH GEMS
                        // =========================================

                        new
                        {
                            name =
                                MarketplaceAgentToolNames
                                    .SearchGems,

                            description =
                                "Search Gemora marketplace gemstone listings. " +
                                "Use this when the buyer wants gemstones based on " +
                                "type, title, color, price, carat range, or sorting. " +
                                "The backend only returns approved and currently " +
                                "available listings.",

                            parameters =
                                new
                                {
                                    type =
                                        "object",

                                    properties =
                                        new
                                        {
                                            search =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    description =
                                                        "Natural search text such as sapphire, ruby, blue sapphire or Ceylon sapphire."
                                                },

                                            gemType =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    description =
                                                        "Gemstone type such as Sapphire, Ruby, Emerald or Spinel."
                                                },

                                            color =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    description =
                                                        "Gemstone color such as blue, yellow, pink or red."
                                                },

                                            minPrice =
                                                new
                                                {
                                                    type =
                                                        "number",

                                                    description =
                                                        "Optional minimum gemstone price."
                                                },

                                            maxPrice =
                                                new
                                                {
                                                    type =
                                                        "number",

                                                    description =
                                                        "Optional maximum gemstone price."
                                                },

                                            minCarat =
                                                new
                                                {
                                                    type =
                                                        "number",

                                                    description =
                                                        "Optional minimum carat weight."
                                                },

                                            maxCarat =
                                                new
                                                {
                                                    type =
                                                        "number",

                                                    description =
                                                        "Optional maximum carat weight."
                                                },

                                            sort =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    description =
                                                        "Optional sorting mode.",

                                                    @enum =
                                                        new[]
                                                        {
                                                            "price_low",
                                                            "price_high",
                                                            "newest",
                                                            "carat_low",
                                                            "carat_high"
                                                        }
                                                }
                                        }
                                }
                        },


                        // =========================================
                        // 2. GEM DETAILS
                        // =========================================

                        new
                        {
                            name =
                                MarketplaceAgentToolNames
                                    .GetGemDetails,

                            description =
                                "Get verified details for one Gemora gemstone listing. " +
                                "Use when the buyer asks about a specific listing.",

                            parameters =
                                new
                                {
                                    type =
                                        "object",

                                    properties =
                                        new
                                        {
                                            gemListingId =
                                                new
                                                {
                                                    type =
                                                        "integer",

                                                    description =
                                                        "Gemora gemstone listing ID."
                                                }
                                        },

                                    required =
                                        new[]
                                        {
                                            "gemListingId"
                                        }
                                }
                        },


                        // =========================================
                        // 3. COMPARE GEMS
                        // =========================================

                        new
                        {
                            name =
                                MarketplaceAgentToolNames
                                    .CompareGems,

                            description =
                                "Compare two or more Gemora gemstone listings " +
                                "using real marketplace data.",

                            parameters =
                                new
                                {
                                    type =
                                        "object",

                                    properties =
                                        new
                                        {
                                            gemListingIds =
                                                new
                                                {
                                                    type =
                                                        "array",

                                                    description =
                                                        "Gem listing IDs to compare.",

                                                    items =
                                                        new
                                                        {
                                                            type =
                                                                "integer"
                                                        }
                                                }
                                        },

                                    required =
                                        new[]
                                        {
                                            "gemListingIds"
                                        }
                                }
                        },


                        // =========================================
                        // 4. MY ORDERS
                        // =========================================

                        new
                        {
                            name =
                                MarketplaceAgentToolNames
                                    .GetMyOrders,

                            description =
                                "Get the authenticated buyer's Gemora orders. " +
                                "Use for purchases, order status, payment status " +
                                "or delivery progress.",

                            parameters =
                                new
                                {
                                    type =
                                        "object",

                                    properties =
                                        new { }
                                }
                        },


                        // =========================================
                        // 5. ORDER DETAILS
                        // =========================================

                        new
                        {
                            name =
                                MarketplaceAgentToolNames
                                    .GetOrderDetails,

                            description =
                                "Get detailed information for one authenticated " +
                                "buyer order, including transaction, fulfillment, " +
                                "delivery and shipment information.",

                            parameters =
                                new
                                {
                                    type =
                                        "object",

                                    properties =
                                        new
                                        {
                                            orderId =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    description =
                                                        "Gemora order UUID."
                                                }
                                        },

                                    required =
                                        new[]
                                        {
                                            "orderId"
                                        }
                                }
                        },


                        // =========================================
                        // 6. SHIPMENT TRACKING
                        // =========================================

                        new
                        {
                            name =
                                MarketplaceAgentToolNames
                                    .GetShipmentTracking,

                            description =
                                "Get courier and shipment tracking information " +
                                "for an authenticated buyer order.",

                            parameters =
                                new
                                {
                                    type =
                                        "object",

                                    properties =
                                        new
                                        {
                                            orderId =
                                                new
                                                {
                                                    type =
                                                        "string",

                                                    description =
                                                        "Gemora order UUID."
                                                }
                                        },

                                    required =
                                        new[]
                                        {
                                            "orderId"
                                        }
                                }
                        }
                    }
            }
        ];
    }


    // ============================================================
    // VALIDATE CONFIGURATION
    // ============================================================

    private void ValidateConfiguration()
    {
        if (
            string.IsNullOrWhiteSpace(
                _options.ApiKey)
        )
        {
            throw new InvalidOperationException(
                "Gemini Marketplace API key is not configured."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                _options.Model)
        )
        {
            throw new InvalidOperationException(
                "Gemini Marketplace model is not configured."
            );
        }
    }


    // ============================================================
    // GEMINI ERROR
    // ============================================================

    private static Exception
        BuildGeminiException(
            HttpStatusCode statusCode,
            string responseBody)
    {
        var message =
            $"Gemini API request failed with status " +
            $"{(int)statusCode} ({statusCode}).";


        try
        {
            using var document =
                JsonDocument.Parse(
                    responseBody);


            if (
                document.RootElement
                    .TryGetProperty(
                        "error",
                        out var errorElement)
                &&
                errorElement
                    .TryGetProperty(
                        "message",
                        out var messageElement)
            )
            {
                var apiMessage =
                    messageElement
                        .GetString();


                if (
                    !string.IsNullOrWhiteSpace(
                        apiMessage)
                )
                {
                    message =
                        $"Gemini API error: {apiMessage}";
                }
            }
        }
        catch
        {
            // Keep safe fallback message.
        }


        // --------------------------------------------------------
        // IMPORTANT:
        //
        // Do NOT use InvalidOperationException for temporary
        // external Gemini API failures.
        //
        // GlobalExceptionHandler previously converted that into
        // HTTP 409 Conflict.
        // --------------------------------------------------------

        return new HttpRequestException(
            message,
            null,
            statusCode
        );
    }
}