using Gemora.Application.DTOs;

namespace Gemora.Application.Interfaces;

public interface IShipmentService
{
    Task<ShipmentResponseDto> CreateShipmentAsync(Guid userId, string userRole, CreateShipmentDto request);
    
    Task<ShipmentResponseDto?> GetShipmentByIdAsync(Guid id);
    
    Task<List<ShipmentResponseDto>> GetShipmentsByOrderIdAsync(Guid orderId);
    
    Task<List<ShipmentResponseDto>> GetUserShipmentsAsync(Guid userId, string userRole);
    
    Task<ShipmentResponseDto> UpdateShipmentStatusAsync(Guid shipmentId, Guid userId, string userRole, UpdateShipmentStatusDto request);
    
    Task<List<TrackingEventResponseDto>> GetTrackingEventsAsync(Guid shipmentId);
    
    Task<TrackingEventResponseDto> AddTrackingEventAsync(Guid shipmentId, Guid userId, string userRole, AddTrackingEventDto request);
    
    Task<InsuranceRecordResponseDto?> GetInsuranceRecordAsync(Guid shipmentId);
    
    Task<InsuranceRecordResponseDto> CreateInsuranceRecordAsync(Guid shipmentId, Guid userId, string userRole, CreateInsuranceRecordRequest request);
}
