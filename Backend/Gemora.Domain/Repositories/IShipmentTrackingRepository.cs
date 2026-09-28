using Gemora.Domain.Entities;

namespace Gemora.Domain.Repositories;

public interface IShipmentTrackingRepository
{
    Task<ShipmentTrackingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ShipmentTrackingEvent>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<ShipmentTrackingEvent?> GetLatestEventByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<ShipmentTrackingEvent> AddAsync(ShipmentTrackingEvent trackingEvent, CancellationToken cancellationToken = default);
}
