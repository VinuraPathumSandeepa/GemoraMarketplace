using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Domain.Repositories;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Shipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Shipment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .Include(s => s.TrackingEvents.OrderBy(t => t.OccurredAt))
            .Include(s => s.InsuranceRecords.OrderByDescending(i => i.CreatedAt))
            .Include(s => s.ShippingPlan)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Shipment?> GetByShipmentNumberAsync(string shipmentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .FirstOrDefaultAsync(s => s.ShipmentNumber == shipmentNumber, cancellationToken);
    }

    public async Task<Shipment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .Include(s => s.TrackingEvents)
            .Include(s => s.InsuranceRecords)
            .Include(s => s.ShippingPlan)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
    }

    public async Task<List<Shipment>> GetBySellerUserIdAsync(Guid sellerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .Where(s => s.SellerUserId == sellerUserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Shipment>> GetByBuyerUserIdAsync(Guid buyerUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .Where(s => s.BuyerUserId == buyerUserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Shipment>> GetByStatusAsync(ShipmentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Shipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Shipment> AddAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync(cancellationToken);
        return shipment;
    }

    public async Task UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        _context.Shipments.Update(shipment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Shipment shipment, CancellationToken cancellationToken = default)
    {
        _context.Shipments.Remove(shipment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .AnyAsync(s => s.OrderId == orderId, cancellationToken);
    }

    public async Task<bool> ExistsByShipmentNumberAsync(string shipmentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Shipments
            .AnyAsync(s => s.ShipmentNumber == shipmentNumber, cancellationToken);
    }
}
