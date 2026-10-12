using Gemora.Domain.Entities;

namespace Gemora.Domain.Constants;

public static class CourierBookingRules
{
    public static bool IsSimulationValue(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.Trim().StartsWith("SIM-", StringComparison.OrdinalIgnoreCase) ||
         value.Trim().StartsWith("MOCK-", StringComparison.OrdinalIgnoreCase) ||
         value.Trim().StartsWith("DEMO", StringComparison.OrdinalIgnoreCase) ||
         value.Contains("sandbox", StringComparison.OrdinalIgnoreCase));

    public static bool IsSimulated(Shipment shipment) =>
        IsSimulationValue(shipment.CourierName) ||
        IsSimulationValue(shipment.TrackingNumber) ||
        IsSimulationValue(shipment.ExternalShipmentReference);

    public static bool HasRealBooking(Shipment shipment) =>
        !IsSimulated(shipment) && !string.IsNullOrWhiteSpace(shipment.CourierName) &&
        !string.IsNullOrWhiteSpace(shipment.TrackingNumber);
}
