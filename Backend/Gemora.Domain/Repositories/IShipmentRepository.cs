using Gemora.Domain.Entities;
using Gemora.Domain.Enums;

namespace Gemora.Domain.Repositories;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByShipmentNumberAsync(string shipmentNumber, CancellationToken cancellationToken = default);
    Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<List<Shipment>> GetBySellerUserIdAsync(Guid sellerUserId, CancellationToken cancellationToken = default);
    Task<List<Shipment>> GetByBuyerUserIdAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
    Task<List<Shipment>> GetByStatusAsync(ShipmentStatus status, CancellationToken cancellationToken = default);
    Task<List<Shipment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Shipment> AddAsync(Shipment shipment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Shipment shipment, CancellationToken cancellationToken = default);
    Task<bool> ExistsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByShipmentNumberAsync(string shipmentNumber, CancellationToken cancellationToken = default);
}
