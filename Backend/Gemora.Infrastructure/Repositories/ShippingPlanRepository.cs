using Gemora.Domain.Entities;
using Gemora.Domain.Repositories;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Infrastructure.Repositories;

public class ShippingPlanRepository : IShippingPlanRepository
{
    private readonly ApplicationDbContext _context;

    public ShippingPlanRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShippingPlan?> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingPlans
            .Include(p => p.Shipment)
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId, cancellationToken);
    }

    public async Task<ShippingPlan> AddAsync(ShippingPlan plan, CancellationToken cancellationToken = default)
    {
        _context.ShippingPlans.Add(plan);
        await _context.SaveChangesAsync(cancellationToken);
        return plan;
    }

    public async Task UpdateAsync(ShippingPlan plan, CancellationToken cancellationToken = default)
    {
        _context.ShippingPlans.Update(plan);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
