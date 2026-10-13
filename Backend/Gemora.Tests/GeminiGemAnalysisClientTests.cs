using System.Net;
using System.Text.Json;
using Gemora.Domain.AI;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.AI;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gemora.Tests;

public class GeminiGemAnalysisClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public Queue<HttpResponseMessage> Responses { get; } = new();
        public List<string> Bodies { get; } = new();
        public List<Uri?> Urls { get; } = new();
        public Exception? Failure { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Urls.Add(request.RequestUri);
            Bodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (Failure is not null)
                throw Failure;
            return Responses.Dequeue();
        }
    }

    private static GeminiGemAnalysisClient Client(FakeHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/") },
            Options.Create(new GeminiOptions { ApiKey = "test-only", Model = "gemini-3.5-flash-lite" }));

    private static GemListing Listing() => new()
    {
        Title = "Blue sapphire", GemType = "Sapphire", CaratWeight = 2,
        Price = 100, Color = "Blue", Clarity = "VVS", Cut = "Oval"
    };

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body) };

    [Fact]
    public async Task FlashLite_RequestIncludesImageAndJsonMode_AndParsesRealResult()
    {
        var handler = new FakeHandler();
        var modelJson = JsonSerializer.Serialize(new
        {
            suggestedGemType = "Sapphire", confidenceScore = 0.8,
            findings = "Visible blue color is consistent with the listing.",
            visualObservations = new[] { "Blue color" }, riskFlags = Array.Empty<string>()
        });
        handler.Responses.Enqueue(JsonResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] {
                new { text = "internal thought", thought = true },
                new { text = modelJson, thought = false }
            } } } } })));

        var result = await Client(handler).AnalyzeAsync(Listing(),
            new MemoryStream(new byte[] { 1, 2, 3 }), "image/png");

        Assert.True(result.ImageAnalyzed);
        Assert.Equal(0.8m, result.ConfidenceScore);
        Assert.Contains("gemini-3.5-flash-lite:generateContent", handler.Urls.Single()!.ToString());
        Assert.DoesNotContain("key=", handler.Urls.Single()!.ToString());
        using var body = JsonDocument.Parse(handler.Bodies.Single());
        var root = body.RootElement;
        Assert.Equal("application/json", root.GetProperty("generationConfig").GetProperty("responseMimeType").GetString());
        Assert.False(root.GetProperty("generationConfig").TryGetProperty("temperature", out _));
        var parts = root.GetProperty("contents")[0].GetProperty("parts");
        Assert.Equal("image/png", parts[1].GetProperty("inlineData").GetProperty("mimeType").GetString());
        Assert.Equal("AQID", parts[1].GetProperty("inlineData").GetProperty("data").GetString());
    }

    [Fact]
    public async Task HardQuota_DoesNotRetry()
    {
        var handler = new FakeHandler();
        handler.Responses.Enqueue(JsonResponse(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"message\":\"Billing account spend cap reached.\"}}"));

        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("QuotaExceeded", error.Category);
        Assert.Single(handler.Bodies);
    }

    [Fact]
    public async Task AmbiguousUsageLimit_IsNotMistakenForConfirmedBillingFailure()
    {
        var handler = new FakeHandler();
        handler.Responses.Enqueue(JsonResponse(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"message\":\"You exceeded your current quota; check your plan and billing details.\"}}"));

        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("UsageLimit", error.Category);
        Assert.Single(handler.Bodies);
    }

    [Fact]
    public async Task QuotaMessageWithShortRetryAfter_DoesNotRetry()
    {
        var handler = new FakeHandler();
        var limited = JsonResponse(HttpStatusCode.TooManyRequests,
            "{\"error\":{\"message\":\"You exceeded your current quota; check your plan and billing details.\"}}");
        limited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(2));
        handler.Responses.Enqueue(limited);

        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("UsageLimit", error.Category);
        Assert.Equal(2, error.RetryAfterSeconds);
        Assert.Single(handler.Bodies);
    }

    [Fact]
    public async Task RateLimit_RespectsRetryAfter_WithoutBlindRetries()
    {
        var handler = new FakeHandler();
        var limited = JsonResponse(HttpStatusCode.TooManyRequests, "{}");
        limited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(20));
        handler.Responses.Enqueue(limited);

        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("RateLimited", error.Category);
        Assert.Equal(20, error.RetryAfterSeconds);
        Assert.Single(handler.Bodies);
    }

    [Fact]
    public async Task ProviderTimeout_IsCategorizedWithoutFabricatingAnalysis()
    {
        var handler = new FakeHandler { Failure = new TaskCanceledException("request timed out") };
        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("Timeout", error.Category);
        Assert.Single(handler.Bodies);
    }

    [Fact]
    public async Task ProviderAuthenticationFailure_DoesNotRetry()
    {
        var handler = new FakeHandler();
        handler.Responses.Enqueue(JsonResponse(HttpStatusCode.Unauthorized, "{}"));
        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("Authentication", error.Category);
        Assert.Single(handler.Bodies);
    }

    [Fact]
    public async Task InvalidRequest_IsNotTreatedAsTemporaryQuota()
    {
        var handler = new FakeHandler();
        handler.Responses.Enqueue(JsonResponse(HttpStatusCode.BadRequest, "{}"));
        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("InvalidRequest", error.Category);
        Assert.Single(handler.Bodies);
    }

    [Fact]
    public async Task EmptyStructuredResult_IsNotReportedAsCompletedAnalysis()
    {
        var handler = new FakeHandler();
        handler.Responses.Enqueue(JsonResponse(HttpStatusCode.OK,
            "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"{}\"}]}}]}"));
        var error = await Assert.ThrowsAsync<GemAiProviderException>(() => Client(handler).AnalyzeAsync(Listing()));
        Assert.Equal("MalformedResponse", error.Category);
    }
}
