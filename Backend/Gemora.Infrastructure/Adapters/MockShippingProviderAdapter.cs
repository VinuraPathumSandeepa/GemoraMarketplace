using Gemora.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gemora.Infrastructure.Adapters;

/// <summary>
/// Mock shipping provider adapter that simulates external courier operations.
/// THIS IS A SIMULATION - NOT A REAL COURIER INTEGRATION.
/// Implements timeout handling, bounded retry, cancellation support, and structured logging.
/// </summary>
public class MockShippingProviderAdapter : IShippingProviderAdapter
{
    private readonly ILogger<MockShippingProviderAdapter> _logger;
    private readonly int _maxRetryAttempts;
    private readonly TimeSpan _timeoutDuration;
    private readonly bool _simulateFailures;

    public MockShippingProviderAdapter(
        ILogger<MockShippingProviderAdapter> logger,
        int maxRetryAttempts = 3,
        int timeoutSeconds = 30,
        bool simulateFailures = false)
    {
        _logger = logger;
        _maxRetryAttempts = maxRetryAttempts;
        _timeoutDuration = TimeSpan.FromSeconds(timeoutSeconds);
        _simulateFailures = simulateFailures;
    }

    public async Task<ShippingLabelResult> GenerateLabelAsync(
        ShippingLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MOCK: Generating shipping label for shipment {ShipmentNumber} from {Origin} to {Destination}",
            request.ShipmentNumber, request.Origin, request.Destination);

        try
        {
            return await ExecuteWithRetryAndTimeout(
                async (ct) =>
                {
                    // Simulate processing time
                    await Task.Delay(TimeSpan.FromMilliseconds(500), ct);

                    // Simulate occasional failures if enabled
                    if (_simulateFailures && ShouldSimulateFailure())
                    {
                        throw new InvalidOperationException("Simulated provider failure");
                    }

                    var trackingNumber = $"MOCK-{Guid.NewGuid():N}".Substring(0, 16).ToUpper();
                    var labelId = $"LABEL-{Guid.NewGuid():N}".Substring(0, 12).ToUpper();

                    _logger.LogInformation(
                        "MOCK: Successfully generated label {LabelId} with tracking number {TrackingNumber}",
                        labelId, trackingNumber);

                    return new ShippingLabelResult
                    {
                        Success = true,
                        LabelId = labelId,
                        LabelUrl = $"https://mock-courier.example.com/labels/{labelId}",
                        TrackingNumber = trackingNumber
                    };
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MOCK: Label generation cancelled for shipment {ShipmentNumber}", request.ShipmentNumber);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Failed to generate label for shipment {ShipmentNumber}", request.ShipmentNumber);
            return new ShippingLabelResult
            {
                Success = false,
                ErrorMessage = $"Label generation failed: {ex.Message}"
            };
        }
    }

    public async Task<RateQuoteResult> GetRateQuoteAsync(
        RateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MOCK: Getting rate quotes from {Origin} to {Destination} for value {DeclaredValue} {Currency}",
            request.Origin, request.Destination, request.DeclaredValue, request.Currency);

        try
        {
            return await ExecuteWithRetryAndTimeout(
                async (ct) =>
                {
                    // Simulate processing time
                    await Task.Delay(TimeSpan.FromMilliseconds(300), ct);

                    // Simulate occasional failures if enabled
                    if (_simulateFailures && ShouldSimulateFailure())
                    {
                        throw new InvalidOperationException("Simulated rate quote failure");
                    }

                    // Generate mock rates based on distance/value simulation
                    var baseRate = CalculateBaseRate(request.DeclaredValue);
                    var rates = new List<RateOption>
                    {
                        new()
                        {
                            ServiceType = "Standard",
                            Price = baseRate,
                            Currency = request.Currency,
                            EstimatedDays = 5,
                            Description = "Standard ground shipping"
                        },
                        new()
                        {
                            ServiceType = "Express",
                            Price = baseRate * 1.5m,
                            Currency = request.Currency,
                            EstimatedDays = 2,
                            Description = "Express air shipping"
                        },
                        new()
                        {
                            ServiceType = "Premium",
                            Price = baseRate * 2.5m,
                            Currency = request.Currency,
                            EstimatedDays = 1,
                            Description = "Premium overnight with insurance"
                        }
                    };

                    _logger.LogInformation("MOCK: Generated {Count} rate options", rates.Count);

                    return new RateQuoteResult
                    {
                        Success = true,
                        Rates = rates
                    };
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MOCK: Rate quote cancelled for route {Origin} to {Destination}", 
                request.Origin, request.Destination);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Failed to get rate quotes");
            return new RateQuoteResult
            {
                Success = false,
                ErrorMessage = $"Rate quote failed: {ex.Message}"
            };
        }
    }

    public async Task<TrackingStatusResult> GetTrackingStatusAsync(
        TrackingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MOCK: Getting tracking status for tracking number {TrackingNumber}",
            request.TrackingNumber);

        try
        {
            return await ExecuteWithRetryAndTimeout(
                async (ct) =>
                {
                    // Simulate processing time
                    await Task.Delay(TimeSpan.FromMilliseconds(200), ct);

                    // Simulate occasional failures if enabled
                    if (_simulateFailures && ShouldSimulateFailure())
                    {
                        throw new InvalidOperationException("Simulated tracking lookup failure");
                    }

                    // Simulate different statuses based on tracking number
                    var statuses = new[] { "In Transit", "Out for Delivery", "Delivered", "Customs Hold" };
                    var locations = new[] { "Colombo Hub", "Kandy Distribution", "Galle Depot", "Airport Facility" };
                    
                    var randomIndex = request.TrackingNumber.GetHashCode() % statuses.Length;
                    if (randomIndex < 0) randomIndex = -randomIndex;

                    _logger.LogInformation("MOCK: Retrieved tracking status: {Status} at {Location}",
                        statuses[randomIndex], locations[randomIndex]);

                    return new TrackingStatusResult
                    {
                        Success = true,
                        CurrentStatus = statuses[randomIndex],
                        Location = locations[randomIndex],
                        LastUpdated = DateTime.UtcNow.AddHours(-randomIndex)
                    };
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("MOCK: Tracking status lookup cancelled for {TrackingNumber}", 
                request.TrackingNumber);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MOCK: Failed to get tracking status for {TrackingNumber}", 
                request.TrackingNumber);
            return new TrackingStatusResult
            {
                Success = false,
                ErrorMessage = $"Tracking lookup failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Executes an operation with bounded retry attempts and timeout handling.
    /// Only retries transient failures.
    /// </summary>
    private async Task<T> ExecuteWithRetryAndTimeout<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) where T : class
    {
        var lastException = default(Exception);

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
                // Timeout - this is a transient failure we can retry
                lastException = ex;
                _logger.LogWarning(
                    "MOCK: Operation timed out on attempt {Attempt}/{MaxAttempts}. Waiting before retry...",
                    attempt, _maxRetryAttempts);

                if (attempt < _maxRetryAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                }
            }
            catch (Exception ex) when (IsTransientFailure(ex))
            {
                // Transient failure - retry
                lastException = ex;
                _logger.LogWarning(
                    ex,
                    "MOCK: Transient failure on attempt {Attempt}/{MaxAttempts}. Waiting before retry...",
                    attempt, _maxRetryAttempts);

                if (attempt < _maxRetryAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Non-transient failure - don't retry
                _logger.LogError(ex, "MOCK: Non-transient failure, not retrying");
                throw;
            }
        }

        // All retries exhausted
        _logger.LogError(lastException, "MOCK: All {MaxAttempts} retry attempts exhausted", _maxRetryAttempts);
        throw new InvalidOperationException(
            $"Operation failed after {_maxRetryAttempts} attempts", lastException);
    }

    /// <summary>
    /// Determines if an exception represents a transient failure that should be retried.
    /// </summary>
    private static bool IsTransientFailure(Exception ex)
    {
        // Network timeouts, temporary service unavailability, etc.
        return ex is TimeoutException ||
               ex is InvalidOperationException ||
               (ex.InnerException != null && IsTransientFailure(ex.InnerException));
    }

    /// <summary>
    /// Randomly determines if a failure should be simulated (for testing).
    /// </summary>
    private static bool ShouldSimulateFailure()
    {
        return new Random().Next(0, 10) < 2; // 20% failure rate
    }

    /// <summary>
    /// Calculates a base rate for mock pricing.
    /// </summary>
    private static decimal CalculateBaseRate(decimal declaredValue)
    {
        // Simple mock calculation - not real pricing logic
        var baseAmount = 25.00m + (declaredValue * 0.02m);
        return Math.Round(baseAmount, 2);
    }
}
