using Gemora.Application.DTOs;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public interface IShipmentService
{
    Task<ShipmentResponseDto> CreateShipmentAsync(Guid userId, string userRole, CreateShipmentDto request);
    Task<ShipmentResponseDto> GetShipmentByIdAsync(Guid id);
    Task<List<ShipmentResponseDto>> GetShipmentsByOrderIdAsync(Guid orderId);
    Task<List<ShipmentResponseDto>> GetUserShipmentsAsync(Guid userId, string userRole);
    Task<ShipmentResponseDto> UpdateShipmentStatusAsync(Guid shipmentId, Guid userId, string userRole, UpdateShipmentStatusDto request);
    Task<InsuranceRecordResponseDto> CreateInsuranceRecordAsync(Guid shipmentId, Guid userId, string userRole, CreateInsuranceRecordRequest request);
    Task<InsuranceRecordResponseDto?> GetInsuranceRecordAsync(Guid shipmentId);
    Task<List<TrackingEventResponseDto>> GetTrackingEventsAsync(Guid shipmentId);
    Task<TrackingEventResponseDto> AddTrackingEventAsync(Guid shipmentId, Guid userId, string userRole, AddTrackingEventDto request);
}

public class ShipmentService : IShipmentService
{
    private readonly ApplicationDbContext _context;

    public ShipmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentResponseDto> CreateShipmentAsync(Guid userId, string userRole, CreateShipmentDto request)
    {
        // Only sellers can create shipments
        if (userRole != "Seller")
        {
            throw new UnauthorizedAccessException("Only sellers can create shipments.");
        }

        // Verify order exists and belongs to this seller
        var order = await _context.Orders
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId);

        if (order == null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        if (order.SellerId != userId)
        {
            throw new UnauthorizedAccessException("You can only create shipments for your own orders.");
        }

        // Check if order is paid
        if (order.Status != "Paid")
        {
            throw new InvalidOperationException("Can only create shipments for paid orders.");
        }

        // Check if shipment already exists for this order
        var existingShipment = await _context.Shipments
            .AnyAsync(s => s.OrderId == request.OrderId);

        if (existingShipment)
        {
            throw new InvalidOperationException("A shipment already exists for this order.");
        }

        // Derive declared value from order if not provided
        var declaredValue = request.DeclaredValue ?? order.TotalAmount;
        var currency = request.Currency ?? order.Currency;

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            SellerId = order.SellerId,
            BuyerId = order.BuyerId,
            OriginAddress = request.OriginAddress,
            OriginRegion = request.OriginRegion,
            OriginCountryCode = request.OriginCountryCode,
            DestinationAddress = request.DestinationAddress,
            DestinationRegion = request.DestinationRegion,
            DestinationCountryCode = request.DestinationCountryCode,
            DeclaredValue = declaredValue,
            Currency = currency,
            PackageDescription = request.PackageDescription,
            PackageWeight = request.PackageWeight,
            PackageDimensions = request.PackageDimensions,
            PreferredService = request.PreferredService ?? "Standard",
            SpecialHandlingNotes = request.SpecialHandlingNotes,
            ExportRequired = request.ExportRequired,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();

        // Create initial tracking event
        var trackingEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            EventType = "Created",
            Location = $"{request.OriginRegion}, {request.OriginCountryCode}",
            Description = "Shipment created",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(trackingEvent);
        await _context.SaveChangesAsync();

        return MapToResponseDto(shipment);
    }

    public async Task<ShipmentResponseDto> GetShipmentByIdAsync(Guid id)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        return MapToResponseDto(shipment);
    }

    public async Task<List<ShipmentResponseDto>> GetShipmentsByOrderIdAsync(Guid orderId)
    {
        var shipments = await _context.Shipments
            .Where(s => s.OrderId == orderId)
            .ToListAsync();

        return shipments.Select(MapToResponseDto).ToList();
    }

    public async Task<List<ShipmentResponseDto>> GetUserShipmentsAsync(Guid userId, string userRole)
    {
        IQueryable<Shipment> query = _context.Shipments;

        // Filter based on role
        if (userRole == "Buyer")
        {
            query = query.Where(s => s.BuyerId == userId);
        }
        else if (userRole == "Seller")
        {
            query = query.Where(s => s.SellerId == userId);
        }
        // Admin sees all shipments

        var shipments = await query.ToListAsync();
        return shipments.Select(MapToResponseDto).ToList();
    }

    public async Task<ShipmentResponseDto> UpdateShipmentStatusAsync(Guid shipmentId, Guid userId, string userRole, UpdateShipmentStatusDto request)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // Authorization check
        if (userRole == "Seller" && shipment.SellerId != userId)
        {
            throw new UnauthorizedAccessException("You can only update your own shipments.");
        }

        if (userRole == "Buyer")
        {
            throw new UnauthorizedAccessException("Buyers cannot update shipment status.");
        }

        // Validate status transition
        ValidateStatusTransition(shipment.Status, request.Status);

        var oldStatus = shipment.Status;
        shipment.Status = request.Status;
        shipment.UpdatedAt = DateTime.UtcNow;

        // Update timestamps based on status
        if (request.Status == "InTransit")
        {
            shipment.ShippedAt = DateTime.UtcNow;
        }
        else if (request.Status == "Delivered")
        {
            shipment.DeliveredAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Add tracking event for status change
        var trackingEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            EventType = $"StatusChanged:{oldStatus}->{request.Status}",
            Location = "System",
            Description = request.Notes ?? $"Status changed from {oldStatus} to {request.Status}",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(trackingEvent);
        await _context.SaveChangesAsync();

        return MapToResponseDto(shipment);
    }

    public async Task<InsuranceRecordResponseDto> CreateInsuranceRecordAsync(Guid shipmentId, Guid userId, string userRole, CreateInsuranceRecordRequest request)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // Only admin or seller can create insurance
        if (userRole == "Buyer")
        {
            throw new UnauthorizedAccessException("Buyers cannot create insurance records.");
        }

        if (userRole == "Seller" && shipment.SellerId != userId)
        {
            throw new UnauthorizedAccessException("You can only create insurance for your own shipments.");
        }

        // Check if insurance already exists
        var existingInsurance = await _context.InsuranceRecords
            .AnyAsync(i => i.ShipmentId == shipmentId);

        if (existingInsurance)
        {
            throw new InvalidOperationException("Insurance record already exists for this shipment.");
        }

        var insuranceRecord = new InsuranceRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            CoverageAmount = request.CoverageAmount,
            Currency = request.Currency ?? shipment.Currency,
            CoverageType = request.CoverageType ?? "Standard",
            Status = "Active",
            PolicyStartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.InsuranceRecords.Add(insuranceRecord);
        await _context.SaveChangesAsync();

        return MapToInsuranceResponseDto(insuranceRecord);
    }

    public async Task<InsuranceRecordResponseDto?> GetInsuranceRecordAsync(Guid shipmentId)
    {
        var insurance = await _context.InsuranceRecords
            .FirstOrDefaultAsync(i => i.ShipmentId == shipmentId);

        return insurance != null ? MapToInsuranceResponseDto(insurance) : null;
    }

    public async Task<List<TrackingEventResponseDto>> GetTrackingEventsAsync(Guid shipmentId)
    {
        var events = await _context.ShipmentTrackingEvents
            .Where(e => e.ShipmentId == shipmentId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync();

        return events.Select(MapToTrackingResponseDto).ToList();
    }

    public async Task<TrackingEventResponseDto> AddTrackingEventAsync(Guid shipmentId, Guid userId, string userRole, AddTrackingEventDto request)
    {
        // Only admin can add tracking events
        if (userRole != "Admin")
        {
            throw new UnauthorizedAccessException("Only administrators can add tracking events.");
        }

        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        var trackingEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            EventType = request.EventType,
            Location = request.Location,
            Description = request.Description,
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(trackingEvent);
        await _context.SaveChangesAsync();

        return MapToTrackingResponseDto(trackingEvent);
    }

    private void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        var validTransitions = new Dictionary<string, List<string>>
        {
            { "Pending", new List<string> { "PlanGenerated", "Cancelled" } },
            { "PlanGenerated", new List<string> { "PlanApproved", "Cancelled" } },
            { "PlanApproved", new List<string> { "InTransit", "Cancelled" } },
            { "InTransit", new List<string> { "Delivered", "Exception" } },
            { "Exception", new List<string> { "InTransit", "Cancelled" } },
            { "Delivered", new List<string>() }, // Terminal state
            { "Cancelled", new List<string>() }  // Terminal state
        };

        if (!validTransitions.ContainsKey(currentStatus))
        {
            throw new InvalidOperationException($"Invalid current status: {currentStatus}");
        }

        if (!validTransitions[currentStatus].Contains(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {currentStatus} to {newStatus}. " +
                $"Valid transitions are: {string.Join(", ", validTransitions[currentStatus])}");
        }
    }

    private static ShipmentResponseDto MapToResponseDto(Shipment shipment)
    {
        return new ShipmentResponseDto
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            SellerId = shipment.SellerId,
            BuyerId = shipment.BuyerId,
            OriginAddress = shipment.OriginAddress,
            OriginRegion = shipment.OriginRegion,
            OriginCountryCode = shipment.OriginCountryCode,
            DestinationAddress = shipment.DestinationAddress,
            DestinationRegion = shipment.DestinationRegion,
            DestinationCountryCode = shipment.DestinationCountryCode,
            DeclaredValue = shipment.DeclaredValue,
            Currency = shipment.Currency,
            PackageDescription = shipment.PackageDescription,
            PackageWeight = shipment.PackageWeight,
            PackageDimensions = shipment.PackageDimensions,
            SpecialHandlingNotes = shipment.SpecialHandlingNotes,
            PreferredService = shipment.PreferredService,
            ExportRequired = shipment.ExportRequired,
            Status = shipment.Status,
            RiskLevel = shipment.RiskLevel,
            TrackingNumber = shipment.TrackingNumber,
            CourierName = shipment.CourierName,
            CreatedAt = shipment.CreatedAt,
            UpdatedAt = shipment.UpdatedAt,
            ShippedAt = shipment.ShippedAt,
            DeliveredAt = shipment.DeliveredAt
        };
    }

    private static InsuranceRecordResponseDto MapToInsuranceResponseDto(InsuranceRecord record)
    {
        return new InsuranceRecordResponseDto
        {
            Id = record.Id,
            ShipmentId = record.ShipmentId,
            CoverageAmount = record.CoverageAmount,
            Currency = record.Currency,
            CoverageType = record.CoverageType,
            PolicyNumber = record.PolicyNumber,
            ProviderName = record.ProviderName,
            PolicyStartDate = record.PolicyStartDate,
            PolicyEndDate = record.PolicyEndDate,
            Status = record.Status,
            CreatedAt = record.CreatedAt
        };
    }

    private static TrackingEventResponseDto MapToTrackingResponseDto(ShipmentTrackingEvent evt)
    {
        return new TrackingEventResponseDto
        {
            Id = evt.Id,
            ShipmentId = evt.ShipmentId,
            EventType = evt.EventType,
            Location = evt.Location,
            Description = evt.Description,
            EventTimestamp = evt.OccurredAt,
            CreatedAt = evt.RecordedAt
        };
    }
}
