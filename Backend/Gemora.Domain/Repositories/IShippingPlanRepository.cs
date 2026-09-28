using Gemora.Domain.Entities;

namespace Gemora.Domain.Repositories;

public interface IShippingPlanRepository
{
    Task<ShippingPlan?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<ShippingPlan> AddAsync(ShippingPlan plan, CancellationToken cancellationToken = default);
    Task UpdateAsync(ShippingPlan plan, CancellationToken cancellationToken = default);
}
