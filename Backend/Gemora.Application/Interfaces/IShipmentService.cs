using Gemora.Application.DTOs;

namespace Gemora.Application.Interfaces;

public interface IShipmentService
{
    Task<ShipmentResponseDto> CreateShipmentAsync(CreateShipmentRequestDto dto, Guid buyerUserId, Guid sellerUserId);

    Task<ShipmentResponseDto?> GetShipmentByIdAsync(Guid shipmentId, Guid currentUserId, string currentUserRole);

    Task<List<ShipmentResponseDto>> GetMyShipmentsAsync(Guid currentUserId, string currentUserRole);

    Task<ShipmentResponseDto> UpdateShipmentStatusAsync(Guid shipmentId, UpdateShipmentStatusRequestDto dto, Guid currentUserId, string currentUserRole);

    Task<InsuranceRecordResponseDto> CreateInsuranceRecordAsync(CreateInsuranceRecordRequestDto dto, Guid currentUserId, string currentUserRole);

    Task<List<InsuranceRecordResponseDto>> GetInsuranceRecordsAsync(Guid shipmentId, Guid currentUserId, string currentUserRole);

    Task<List<TrackingEventDto>> GetTrackingEventsAsync(Guid shipmentId, Guid currentUserId, string currentUserRole);

    Task<ShippingPlanResponseDto> CreateShippingPlanAsync(Guid shipmentId, Guid currentUserId, string currentUserRole);
}
