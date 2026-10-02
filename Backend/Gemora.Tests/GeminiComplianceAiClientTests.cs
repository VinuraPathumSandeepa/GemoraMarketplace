using System.Net;
using System.Text.Json;
using Gemora.API.Services;
using Gemora.Application.Configuration;
using Gemora.Application.DTOs.ExportCompliance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gemora.Tests;

public class GeminiComplianceAiClientTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public List<HttpRequestMessage> Requests { get; } = new();

        public void QueueResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            };
            _responses.Enqueue(response);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (_responses.Count > 0)
            {
                return await Task.FromResult(_responses.Dequeue());
            }

            return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"error\": {\"code\": 503, \"message\": \"Service Unavailable\", \"status\": \"UNAVAILABLE\"}}")
            });
        }
    }

    private static GeminiComplianceAiClient CreateClient(FakeHttpMessageHandler handler, GeminiComplianceOptions options)
    {
        var httpClient = new HttpClient(handler);
        var optionsMock = Options.Create(options);
        var configBuilder = new ConfigurationBuilder();
        var config = configBuilder.Build();
        var logger = NullLogger<GeminiComplianceAiClient>.Instance;

        return new GeminiComplianceAiClient(httpClient, optionsMock, config, logger);
    }

    private static ComplianceAgentContextDto CreateValidContext()
    {
        return new ComplianceAgentContextDto
        {
            ExportRequest = new ComplianceAgentExportRequestDto
            {
                ExportRequestId = Guid.NewGuid(),
                OriginCountry = "BWA",
                DestinationCountry = "USA",
                DeclaredValue = 25000,
                Currency = "USD",
                Status = "Submitted"
            },
            DeterministicCheck = new ComplianceCheckResultDto
            {
                IsComplete = true,
                MissingRequirements = new List<string>()
            },
            Documents = new List<ComplianceAgentDocumentDto>()
        };
    }

    private static string CreateValidGeminiResponseBody(string summary = "Compliance review advisory note.")
    {
        var assessmentJson = JsonSerializer.Serialize(new
        {
            summary = summary,
            deterministicComplete = true,
            missingRequirements = new string[0],
            documentFindings = new object[0],
            inconsistencies = new string[0],
            warnings = new string[0],
            recommendedOfficerChecks = new[] { "Check invoice authenticity" },
            requiresOfficerAttention = false,
            confidence = 0.95
        });

        var geminiResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = assessmentJson }
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(geminiResponse);
    }

    // A. Primary model succeeds immediately
    [Fact]
    public async Task AnalyzeAsync_PrimaryModelSucceedsImmediately_ReturnsSuccessWithPrimaryModel()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, CreateValidGeminiResponseBody("Primary success"));

        var options = new GeminiComplianceOptions
        {
            ApiKey = "fake-api-key",
            Model = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite"
        };

        var client = CreateClient(handler, options);
        var context = CreateValidContext();

        var result = await client.AnalyzeAsync(context);

        Assert.True(result.Success);
        Assert.Equal("gemini-3.8-flash", result.ModelName);
        Assert.Single(handler.Requests);
        Assert.Contains("models/gemini-3.8-flash:generateContent", handler.Requests[0].RequestUri?.ToString());
    }

    // B. Primary returns 503 once, then succeeds
    [Fact]
    public async Task AnalyzeAsync_PrimaryReturns503OnceThenSucceeds_RetriesAndSucceedsWithPrimaryModel()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "{\"error\": {\"code\": 503, \"message\": \"High load\", \"status\": \"UNAVAILABLE\"}}");
        handler.QueueResponse(HttpStatusCode.OK, CreateValidGeminiResponseBody("Primary retry success"));

        var options = new GeminiComplianceOptions
        {
            ApiKey = "fake-api-key",
            Model = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite"
        };

        var client = CreateClient(handler, options);
        var context = CreateValidContext();

        var result = await client.AnalyzeAsync(context);

        Assert.True(result.Success);
        Assert.Equal("gemini-3.8-flash", result.ModelName);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("models/gemini-3.8-flash:generateContent", handler.Requests[0].RequestUri?.ToString());
        Assert.Contains("models/gemini-3.8-flash:generateContent", handler.Requests[1].RequestUri?.ToString());
    }

    // C. Primary repeatedly returns 503 and fallback succeeds
    [Fact]
    public async Task AnalyzeAsync_PrimaryRepeatedlyFails503_FallbackSucceeds()
    {
        var handler = new FakeHttpMessageHandler();
        // Primary 3 attempts -> 503
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "{\"error\": {\"code\": 503, \"message\": \"Unavailable\", \"status\": \"UNAVAILABLE\"}}");
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "{\"error\": {\"code\": 503, \"message\": \"Unavailable\", \"status\": \"UNAVAILABLE\"}}");
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "{\"error\": {\"code\": 503, \"message\": \"Unavailable\", \"status\": \"UNAVAILABLE\"}}");
        // Fallback attempt 1 -> 200 OK
        handler.QueueResponse(HttpStatusCode.OK, CreateValidGeminiResponseBody("Fallback success"));

        var options = new GeminiComplianceOptions
        {
            ApiKey = "fake-api-key",
            Model = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite"
        };

        var client = CreateClient(handler, options);
        var context = CreateValidContext();

        var result = await client.AnalyzeAsync(context);

        Assert.True(result.Success);
        Assert.Equal("gemini-3.5-flash-lite", result.ModelName);
        Assert.Equal(4, handler.Requests.Count);
        Assert.Contains("models/gemini-3.8-flash:generateContent", handler.Requests[0].RequestUri?.ToString());
        Assert.Contains("models/gemini-3.8-flash:generateContent", handler.Requests[1].RequestUri?.ToString());
        Assert.Contains("models/gemini-3.8-flash:generateContent", handler.Requests[2].RequestUri?.ToString());
        Assert.Contains("models/gemini-3.5-flash-lite:generateContent", handler.Requests[3].RequestUri?.ToString());
    }

    // D. Both primary and fallback repeatedly return 503 -> safe provider failure
    [Fact]
    public async Task AnalyzeAsync_BothPrimaryAndFallbackRepeatedlyFail503_ReturnsSafeFailure()
    {
        var handler = new FakeHttpMessageHandler();
        // Primary 3 attempts -> 503
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "503");
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "503");
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "503");
        // Fallback 3 attempts -> 503
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "503");
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "503");
        handler.QueueResponse(HttpStatusCode.ServiceUnavailable, "503");

        var options = new GeminiComplianceOptions
        {
            ApiKey = "fake-api-key",
            Model = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite"
        };

        var client = CreateClient(handler, options);
        var context = CreateValidContext();

        var result = await client.AnalyzeAsync(context);

        Assert.False(result.Success);
        Assert.Equal("AI_PROVIDER_ERROR", result.ErrorCode);
        Assert.True(result.IsTransientFailure);
        Assert.Equal(6, handler.Requests.Count);
    }

    // E. HTTP 400 is NOT blindly retried
    [Fact]
    public async Task AnalyzeAsync_Http400_IsNotRetriedAndDoesNotUseFallback()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.BadRequest, "{\"error\": {\"code\": 400, \"message\": \"Invalid Argument\", \"status\": \"INVALID_ARGUMENT\"}}");

        var options = new GeminiComplianceOptions
        {
            ApiKey = "fake-api-key",
            Model = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite"
        };

        var client = CreateClient(handler, options);
        var context = CreateValidContext();

        var result = await client.AnalyzeAsync(context);

        Assert.False(result.Success);
        Assert.Equal("AI_PROVIDER_ERROR", result.ErrorCode);
        Assert.False(result.IsTransientFailure);
        Assert.Single(handler.Requests);
    }

    // F. Malformed AI output is NOT accepted just because fallback/retry exists
    [Fact]
    public async Task AnalyzeAsync_MalformedOutput_FailsWithoutBlindRetry()
    {
        var handler = new FakeHttpMessageHandler();
        // Return 200 OK but invalid JSON inside candidates text
        var malformedGeminiResponse = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new { text = "{ invalid json structure" }
                        }
                    }
                }
            }
        };
        handler.QueueResponse(HttpStatusCode.OK, JsonSerializer.Serialize(malformedGeminiResponse));

        var options = new GeminiComplianceOptions
        {
            ApiKey = "fake-api-key",
            Model = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite"
        };

        var client = CreateClient(handler, options);
        var context = CreateValidContext();

        var result = await client.AnalyzeAsync(context);

        Assert.False(result.Success);
        Assert.Equal("AI_INVALID_RESPONSE", result.ErrorCode);
        Assert.False(result.IsTransientFailure);
        Assert.Single(handler.Requests);
    }

    // G. AI still cannot directly approve/reject an export
    [Fact]
    public async Task AnalyzeAsync_SuccessfulAssessment_ContainsRequiredDisclaimerAndAdvisoryRole()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, CreateValidGeminiResponseBody("Advisory analysis complete"));

        var options = new GeminiComplianceOptions
        {
            ApiKey = "fake-api-key",
            Model = "gemini-3.8-flash",
            FallbackModel = "gemini-3.5-flash-lite"
        };

        var client = CreateClient(handler, options);
        var context = CreateValidContext();

        var result = await client.AnalyzeAsync(context);

        Assert.True(result.Success);
        Assert.NotNull(result.Result);
        Assert.Contains("Export Officer", result.Result.Disclaimer);
        Assert.False(result.Result.RequiresOfficerAttention);
    }
}
