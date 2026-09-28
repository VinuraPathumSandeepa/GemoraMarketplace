using Gemora.Domain.Entities;
using Gemora.Domain.Enums;

namespace Gemora.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithUsersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Order>> GetByBuyerUserIdAsync(Guid buyerUserId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetBySellerUserIdAsync(Guid sellerUserId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken = default);
    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
