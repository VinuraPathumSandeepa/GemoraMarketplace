using Gemora.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gemora.Infrastructure.Providers;

/// <summary>
/// Mock LLM provider for development/testing. Simulates AI-powered risk assessment
/// with configurable failure scenarios to test fallback mechanisms.
/// In production, replace with real Gemini/OpenAI/Azure OpenAI provider.
/// </summary>
public class MockLlmProvider : ILlmProvider
{
    private readonly ILogger<MockLlmProvider> _logger;
    private readonly int _maxRetryAttempts;
    private readonly TimeSpan _timeoutDuration;
    private readonly bool _simulateFailures;
    private readonly Random _random = new();

    public MockLlmProvider(
        ILogger<MockLlmProvider> logger,
        int maxRetryAttempts = 3,
        int timeoutSeconds = 30,
        bool simulateFailures = false)
    {
        _logger = logger;
        _maxRetryAttempts = maxRetryAttempts;
        _timeoutDuration = TimeSpan.FromSeconds(timeoutSeconds);
        _simulateFailures = simulateFailures;
    }

    public async Task<LlmShippingPlanRecommendation?> GenerateShippingPlanAsync(
        LlmShippingPlanInput input,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAndTimeout(async (ct) =>
        {
            _logger.LogInformation("Mock LLM generating plan for shipment {ShipmentId}", input.ShipmentId);

            // Simulate network delay
            await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(200, 800)), ct);

            // Simulate failures if configured
            if (_simulateFailures && _random.Next(0, 100) < 30) // 30% failure rate
            {
                throw new InvalidOperationException("Simulated LLM provider failure");
            }

            // Generate deterministic "AI-like" response based on input rules
            var recommendation = GenerateAiLikeRecommendation(input);

            _logger.LogInformation("Mock LLM generated plan: Risk={RiskLevel}, Service={ServiceType}",
                recommendation.RiskLevel, recommendation.RecommendedServiceType);

            return recommendation;
        }, cancellationToken);
    }

    private LlmShippingPlanRecommendation GenerateAiLikeRecommendation(LlmShippingPlanInput input)
    {
        // Calculate risk score similar to deterministic planner but with "AI reasoning"
        int riskScore = 0;
        var reasons = new List<string>();

        // Value-based risk
        if (input.DeclaredValue > 10000)
        {
            riskScore += 3;
            reasons.Add($"Exceptional value (${input.DeclaredValue:N0}) requires enhanced protection");
        }
        else if (input.DeclaredValue > 5000)
        {
            riskScore += 2;
            reasons.Add($"High-value shipment (${input.DeclaredValue:N0}) warrants premium service");
        }
        else if (input.DeclaredValue > 1000)
        {
            riskScore += 1;
            reasons.Add($"Moderate value (${input.DeclaredValue:N0}) suggests standard insurance");
        }

        // International complexity
        if (input.OriginCountryCode != input.DestinationCountryCode)
        {
            riskScore += 2;
            reasons.Add($"Cross-border shipment ({input.OriginCountryCode}→{input.DestinationCountryCode}) introduces customs risk");
        }

        // Export requirements
        if (input.ExportRequired)
        {
            riskScore += 1;
            reasons.Add("Export documentation adds processing complexity");
        }

        // Weight considerations
        if (input.PackageWeight.HasValue && input.PackageWeight.Value > 5)
        {
            riskScore += 1;
            reasons.Add($"Heavy package ({input.PackageWeight.Value}kg) requires special handling");
        }

        // Special handling
        if (!string.IsNullOrEmpty(input.SpecialHandlingNotes))
        {
            riskScore += 1;
            reasons.Add("Special handling requirements noted");
        }

        // Map to risk level
        string riskLevel = riskScore switch
        {
            >= 6 => "Critical",
            >= 4 => "High",
            >= 2 => "Medium",
            _ => "Low"
        };

        // Determine service type
        string serviceType = riskLevel switch
        {
            "Critical" or "High" => "Express Insured",
            _ when input.ExportRequired => "International Priority",
            _ when input.DeclaredValue > 2000 => "Priority Insured",
            _ => "Standard"
        };

        // Validate against approved services
        if (input.ApprovedServiceTypes.Any() && !input.ApprovedServiceTypes.Contains(serviceType))
        {
            serviceType = input.ApprovedServiceTypes.First();
            reasons.Add($"Adjusted to approved service: {serviceType}");
        }

        // Coverage calculation
        decimal? coverageAmount = riskLevel switch
        {
            "Critical" => input.DeclaredValue * 1.2m,
            "High" => input.DeclaredValue * 1.1m,
            "Medium" when input.DeclaredValue > 1000 => input.DeclaredValue,
            _ => null
        };

        // Handling requirements
        var handlingRequirements = new List<string>();
        if (riskLevel is "Critical" or "High")
        {
            handlingRequirements.Add("Signature required on delivery");
            handlingRequirements.Add("Tamper-evident packaging verification");
        }
        if (input.ExportRequired)
        {
            handlingRequirements.Add("Customs documentation pre-attached");
        }
        if (riskLevel == "Critical")
        {
            handlingRequirements.Add("GPS tracking enabled throughout transit");
        }

        // Required documents
        var documents = new List<string> { "Commercial invoice", "Packing list" };
        if (input.ExportRequired)
        {
            documents.Add("Export declaration");
            documents.Add("Certificate of origin");
        }
        if (!string.IsNullOrEmpty(input.GemType))
        {
            documents.Add($"{input.GemType} certificate of authenticity");
        }

        // Warnings
        var warnings = new List<string>();
        if (input.OriginCountryCode != input.DestinationCountryCode)
        {
            warnings.Add("Customs clearance may add 2-5 business days");
        }
        if (riskLevel == "Critical")
        {
            warnings.Add("Enhanced security protocols active for high-value shipment");
        }

        return new LlmShippingPlanRecommendation
        {
            RiskLevel = riskLevel,
            RiskReasons = reasons,
            RecommendedServiceType = serviceType,
            InsuranceRecommended = input.DeclaredValue > 1000 || riskLevel is "High" or "Critical",
            RecommendedCoverageAmount = coverageAmount,
            HandlingRequirements = handlingRequirements,
            RequiredDocuments = documents,
            Warnings = warnings,
            GenerationSource = "AI",
            ExecutionSummary = $"AI analysis: {reasons.Count} risk factors identified, {handlingRequirements.Count} handling requirements, {documents.Count} documents needed"
        };
    }

    private async Task<T?> ExecuteWithRetryAndTimeout<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) where T : class
    {
        for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeoutDuration);

                return await operation(cts.Token);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
            {
                _logger.LogWarning(ex, "LLM provider timeout on attempt {Attempt}/{MaxAttempts}", attempt, _maxRetryAttempts);

                if (attempt == _maxRetryAttempts)
                {
                    _logger.LogError("LLM provider failed after {MaxAttempts} attempts due to timeout", _maxRetryAttempts);
                    return null;
                }
            }
            catch (Exception ex) when (IsTransientFailure(ex))
            {
                _logger.LogWarning(ex, "LLM provider transient failure on attempt {Attempt}/{MaxAttempts}", attempt, _maxRetryAttempts);

                if (attempt == _maxRetryAttempts)
                {
                    _logger.LogError(ex, "LLM provider failed after {MaxAttempts} attempts", _maxRetryAttempts);
                    return null;
                }

                // Exponential backoff
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt - 1) * 500), cancellationToken);
            }
            catch (Exception ex)
            {
                // Non-transient failure - don't retry
                _logger.LogError(ex, "LLM provider non-transient failure");
                throw;
            }
        }

        return null;
    }

    private static bool IsTransientFailure(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TimeoutException ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase);
    }
}
