using Gemora.Domain.Interfaces;

namespace Gemora.Infrastructure.Adapters;

// Manual bookings use courier-issued details. No external booking is fabricated.
public sealed class ManualShippingProviderAdapter : IShippingProviderAdapter
{
    private static Task<T> NotConfigured<T>(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<T>(new InvalidOperationException(
            "Direct courier API booking is not configured. Record the courier-issued booking details instead."));
    }

    public Task<ShippingLabelResult> GenerateLabelAsync(ShippingLabelRequest request, CancellationToken cancellationToken = default)
        => NotConfigured<ShippingLabelResult>(cancellationToken);
    public Task<RateQuoteResult> GetRateQuoteAsync(RateQuoteRequest request, CancellationToken cancellationToken = default)
        => NotConfigured<RateQuoteResult>(cancellationToken);
    public Task<TrackingStatusResult> GetTrackingStatusAsync(TrackingStatusRequest request, CancellationToken cancellationToken = default)
        => NotConfigured<TrackingStatusResult>(cancellationToken);
    public Task<CourierBookingResult> BookShipmentAsync(CourierBookingRequest request, CancellationToken cancellationToken = default)
        => NotConfigured<CourierBookingResult>(cancellationToken);
}
