namespace Gemora.Domain.Interfaces;

/// <summary>
/// Interface for shipping provider adapter that simulates external courier operations.
/// This is a MOCK implementation - not a real courier integration.
/// </summary>
public interface IShippingProviderAdapter
{
    /// <summary>
    /// Generates a simulated shipping label without making real courier bookings.
    /// </summary>
    Task<ShippingLabelResult> GenerateLabelAsync(
        ShippingLabelRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtains shipping rate quotes from the simulated provider.
    /// </summary>
    Task<RateQuoteResult> GetRateQuoteAsync(
        RateQuoteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Simulates tracking status updates or retrieves current tracking information.
    /// </summary>
    Task<TrackingStatusResult> GetTrackingStatusAsync(
        TrackingStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Books a shipment with the simulated courier provider.
    /// THIS IS A SIMULATION - NOT A REAL COURIER BOOKING.
    /// </summary>
    Task<CourierBookingResult> BookShipmentAsync(
        CourierBookingRequest request,
        CancellationToken cancellationToken = default);
}

// Request/Response Models

public class ShippingLabelRequest
{
    public string ShipmentNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string PackageDescription { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
}

public class ShippingLabelResult
{
    public bool Success { get; set; }
    public string? LabelId { get; set; }
    public string? LabelUrl { get; set; }
    public string? TrackingNumber { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RateQuoteRequest
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ServiceType { get; set; }
}

public class RateQuoteResult
{
    public bool Success { get; set; }
    public List<RateOption> Rates { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class RateOption
{
    public string ServiceType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int EstimatedDays { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class TrackingStatusRequest
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
}

public class TrackingStatusResult
{
    public bool Success { get; set; }
    public string? CurrentStatus { get; set; }
    public string? Location { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? ErrorMessage { get; set; }
}

// Booking Models (Phase 3)

public class CourierBookingRequest
{
    public string ShipmentNumber { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string PackageDescription { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public decimal? Weight { get; set; }
}

public class CourierBookingResult
{
    public bool Success { get; set; }
    public string? CourierName { get; set; }
    public string? ExternalShipmentReference { get; set; }
    public string? TrackingNumber { get; set; }
    public string? SelectedService { get; set; }
    public string? ErrorMessage { get; set; }
}
