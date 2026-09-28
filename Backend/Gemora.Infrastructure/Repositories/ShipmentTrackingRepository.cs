using Gemora.Domain.Entities;
using Gemora.Domain.Repositories;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Infrastructure.Repositories;

public class ShipmentTrackingRepository : IShipmentTrackingRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentTrackingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentTrackingEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentTrackingEvents
            .Include(t => t.Shipment)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<ShipmentTrackingEvent>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentTrackingEvents
            .Where(t => t.ShipmentId == shipmentId)
            .OrderBy(t => t.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShipmentTrackingEvent?> GetLatestEventByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ShipmentTrackingEvents
            .Where(t => t.ShipmentId == shipmentId)
            .OrderByDescending(t => t.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ShipmentTrackingEvent> AddAsync(ShipmentTrackingEvent trackingEvent, CancellationToken cancellationToken = default)
    {
        _context.ShipmentTrackingEvents.Add(trackingEvent);
        await _context.SaveChangesAsync(cancellationToken);
        return trackingEvent;
    }
}
