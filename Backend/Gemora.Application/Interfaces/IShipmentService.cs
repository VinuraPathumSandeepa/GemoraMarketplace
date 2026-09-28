using Gemora.Application.DTOs;
using Gemora.Domain.Enums;

namespace Gemora.Application.Interfaces;

/// <summary>
/// Business service for managing shipments including creation, retrieval, status transitions, and admin approval.
/// Enforces ownership validation, authorization rules, and business invariants.
/// </summary>
public interface IShipmentService
{
    /// <summary>
    /// Creates a new shipment after validating order eligibility and seller ownership.
    /// </summary>
    Task<ShipmentDto> CreateShipmentAsync(
        CreateShipmentDto request,
        Guid authenticatedUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a shipment by ID with authorization checks.
    /// </summary>
    Task<ShipmentDto?> GetShipmentByIdAsync(
        Guid shipmentId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a shipment by Order ID with authorization checks.
    /// </summary>
    Task<ShipmentDto?> GetShipmentByOrderIdAsync(
        Guid orderId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all shipments for a specific user based on their role.
    /// </summary>
    Task<List<ShipmentDto>> GetUserShipmentsAsync(
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates shipment status with validation of allowed transitions.
    /// </summary>
    Task<ShipmentDto> UpdateShipmentStatusAsync(
        Guid shipmentId,
        ShipmentStatus newStatus,
        Guid authenticatedUserId,
        string userRole,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a shipping plan (Admin only).
    /// Validates the plan against business rules before approval.
    /// </summary>
    Task<ShippingPlanDto> ApproveShippingPlanAsync(
        Guid shipmentId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the shipping plan for a shipment.
    /// </summary>
    Task<ShippingPlanDto?> GetShippingPlanAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a shipping plan using the four-agent AI subsystem.
    /// </summary>
    Task<ShippingPlanGenerationResult> GenerateShippingPlanAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tracking events for a shipment with authorization checks.
    /// </summary>
    Task<List<TrackingEventDto>> GetShipmentTrackingEventsAsync(
        Guid shipmentId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves insurance records for a shipment with authorization checks.
    /// </summary>
    Task<List<InsuranceRecordDto>> GetShipmentInsuranceRecordsAsync(
        Guid shipmentId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a tracking event to a shipment (Admin only).
    /// </summary>
    Task<TrackingEventDto> AddTrackingEventAsync(
        Guid shipmentId,
        CreateTrackingEventRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an insurance record for a shipment (Admin only).
    /// </summary>
    Task<InsuranceRecordDto> CreateInsuranceRecordAsync(
        Guid shipmentId,
        CreateInsuranceRecordRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default);
}
