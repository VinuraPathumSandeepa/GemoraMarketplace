using Gemora.Application.DTOs;

namespace Gemora.Application.Interfaces;

public interface IShippingAgentService
{
    Task<ShippingPlanResponseDto> GenerateShippingPlanAsync(Guid shipmentId);
    
    Task<bool> ApproveShippingPlanAsync(Guid shipmentId, Guid adminId, string? adminNotes = null);
}
