using Gemora.Domain.Entities;
using Gemora.Domain.Repositories;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Infrastructure.Repositories;

public class InsuranceRepository : IInsuranceRepository
{
    private readonly ApplicationDbContext _context;

    public InsuranceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InsuranceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.InsuranceRecords
            .Include(i => i.Shipment)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<List<InsuranceRecord>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.InsuranceRecords
            .Where(i => i.ShipmentId == shipmentId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<InsuranceRecord?> GetByPolicyReferenceAsync(string policyReference, CancellationToken cancellationToken = default)
    {
        return await _context.InsuranceRecords
            .Include(i => i.Shipment)
            .FirstOrDefaultAsync(i => i.PolicyReference == policyReference, cancellationToken);
    }

    public async Task<InsuranceRecord> AddAsync(InsuranceRecord record, CancellationToken cancellationToken = default)
    {
        _context.InsuranceRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task UpdateAsync(InsuranceRecord record, CancellationToken cancellationToken = default)
    {
        _context.InsuranceRecords.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(InsuranceRecord record, CancellationToken cancellationToken = default)
    {
        _context.InsuranceRecords.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByPolicyReferenceAsync(string policyReference, CancellationToken cancellationToken = default)
    {
        return await _context.InsuranceRecords
            .AnyAsync(i => i.PolicyReference == policyReference, cancellationToken);
    }
}
