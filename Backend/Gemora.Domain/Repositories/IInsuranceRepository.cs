using Gemora.Domain.Entities;

namespace Gemora.Domain.Repositories;

public interface IInsuranceRepository
{
    Task<InsuranceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<InsuranceRecord>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<InsuranceRecord?> GetByPolicyReferenceAsync(string policyReference, CancellationToken cancellationToken = default);
    Task<InsuranceRecord> AddAsync(InsuranceRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(InsuranceRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(InsuranceRecord record, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPolicyReferenceAsync(string policyReference, CancellationToken cancellationToken = default);
}
