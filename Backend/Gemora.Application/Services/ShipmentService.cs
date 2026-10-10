using Gemora.Application.DTOs;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
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
    Task<InsuranceRecordResponseDto?> GetInsuranceRecordAsync(Guid shipmentId, Guid userId, string userRole);
    Task<List<TrackingEventResponseDto>> GetTrackingEventsAsync(Guid shipmentId, Guid userId, string userRole);
    Task<TrackingEventResponseDto> AddTrackingEventAsync(Guid shipmentId, Guid userId, string userRole, AddTrackingEventDto request);
    Task<CourierBookingResult> BookShipmentAsync(Guid shipmentId, Guid userId, string userRole);
}

public class ShipmentService : IShipmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IShippingProviderAdapter _shippingProvider;

    public ShipmentService(ApplicationDbContext context, IShippingProviderAdapter shippingProvider)
    {
        _context = context;
        _shippingProvider = shippingProvider;
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

        // The paid order is authoritative for risk assessment and insurance.
        if (order.TotalAmount <= 0 || string.IsNullOrWhiteSpace(order.Currency))
            throw new InvalidOperationException("The paid order must have a positive total and a currency before shipment creation.");
        if (request.DeclaredValue.HasValue && request.DeclaredValue.Value != order.TotalAmount)
            throw new InvalidOperationException($"Declared value must match the paid order total ({order.Currency} {order.TotalAmount}).");
        if (request.Currency != null && !string.Equals(
            request.Currency.Trim(), order.Currency.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Currency must match the paid order currency ({order.Currency}).");
        var declaredValue = order.TotalAmount;
        var currency = order.Currency;

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

        // Admin-only operations: Only Admin can mark as Delivered or handle Exceptions
        if (request.Status == "Delivered" || request.Status == "Exception" || request.Status == "DeliveryFailed" || request.Status == "CustomsHold")
        {
            if (userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Only administrators can perform this operational status change.");
            }
        }

        // Prevent booking before plan approval
        if (request.Status == "ReadyForBooking")
        {
            if (userRole != "Admin")
            {
                throw new UnauthorizedAccessException("Only administrators can set shipment status to ReadyForBooking.");
            }

            // Verify plan is approved
            var plan = await _context.ShippingPlans
                .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

            if (plan == null)
            {
                throw new InvalidOperationException("Cannot book shipment: No shipping plan has been generated.");
            }

            if (!plan.IsApproved)
            {
                throw new InvalidOperationException("Cannot book shipment: Shipping plan has not been approved by Admin.");
            }
        }

        // Validate status transition
        ValidateStatusTransition(shipment.Status, request.Status);

        var oldStatus = shipment.Status;
        shipment.Status = request.Status;
        shipment.UpdatedAt = DateTime.UtcNow;

        // Update timestamps based on status
        if (request.Status == "InTransit" || request.Status == "PickedUp")
        {
            shipment.ShippedAt = DateTime.UtcNow;
        }
        else if (request.Status == "Delivered")
        {
            shipment.DeliveredAt = DateTime.UtcNow;
        }

        // Create tracking event WITHIN SAME TRANSACTION
        var trackingEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            EventType = request.Status,
            Location = request.Location ?? "System",
            Description = request.Notes ?? $"Status changed from {oldStatus} to {request.Status}",
            PerformedByUserId = userId,
            PerformedByRole = userRole,
            PreviousState = oldStatus,
            NewState = request.Status,
            Reason = request.Notes,
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(trackingEvent);

        // SINGLE SaveChanges ensures atomic transaction - both shipment and tracking event saved together
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

        // Only Admin can create insurance records (Phase 4 requirement)
        if (userRole != "Admin")
        {
            throw new UnauthorizedAccessException("Only administrators can create insurance records.");
        }

        // Check if insurance already exists
        var existingInsurance = await _context.InsuranceRecords
            .AnyAsync(i => i.ShipmentId == shipmentId);

        if (existingInsurance)
        {
            throw new InvalidOperationException("Insurance record already exists for this shipment.");
        }

        // VALIDATION: CoverageAmount > 0
        if (request.CoverageAmount <= 0)
        {
            throw new InvalidOperationException("Coverage amount must be greater than zero.");
        }

        // VALIDATION: DeclaredValue > 0
        if (request.DeclaredValue <= 0)
        {
            throw new InvalidOperationException("Declared value must be greater than zero.");
        }

        // VALIDATION: PremiumAmount >= 0 (calculated from coverage)
        var premiumAmount = CalculatePremium(request.CoverageAmount, request.CoverageType ?? "Standard");
        if (premiumAmount < 0)
        {
            throw new InvalidOperationException("Premium calculation failed.");
        }

        // VALIDATION: Currency matches Shipment
        var currency = request.Currency ?? shipment.Currency;
        if (currency != shipment.Currency)
        {
            throw new InvalidOperationException($"Currency must match shipment currency ({shipment.Currency}).");
        }

        // Generate SIM-prefixed policy reference for clear demo labeling
        var policyReference = $"SIM-POL-{Guid.NewGuid():N}".Substring(0, 16).ToUpper();
        var providerName = request.ProviderName ?? "DEMO Gemora Insurance Sandbox";

        var insuranceRecord = new InsuranceRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            DeclaredValue = request.DeclaredValue,
            CoverageAmount = request.CoverageAmount,
            Currency = currency,
            CoverageType = request.CoverageType ?? "Standard",
            PolicyNumber = policyReference,
            PolicyReference = policyReference,
            ProviderName = providerName,
            PremiumAmount = premiumAmount,
            Status = "Active",
            PolicyStartDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.InsuranceRecords.Add(insuranceRecord);
        
        // Create audit tracking event for insurance creation
        var insuranceEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            EventType = "InsuranceCreated",
            Location = "Admin Insurance Management",
            Description = $"Insurance created: Policy {policyReference}, Coverage {currency} {request.CoverageAmount}",
            PerformedByUserId = userId,
            PerformedByRole = userRole,
            PreviousState = "No Insurance",
            NewState = "Insured",
            Reason = $"Coverage: {currency} {request.CoverageAmount}, Type: {insuranceRecord.CoverageType}",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };
        
        _context.ShipmentTrackingEvents.Add(insuranceEvent);
        
        await _context.SaveChangesAsync();

        return MapToInsuranceResponseDto(insuranceRecord);
    }

    private static decimal CalculatePremium(decimal coverageAmount, string coverageType)
    {
        // Simple mock premium calculation - not real actuarial logic
        var baseRate = coverageType switch
        {
            "Premium" => 0.05m,      // 5% of coverage
            "Comprehensive" => 0.08m, // 8% of coverage
            _ => 0.03m                // 3% for Standard
        };

        var premium = coverageAmount * baseRate;
        return Math.Round(premium, 2);
    }

    public async Task<InsuranceRecordResponseDto?> GetInsuranceRecordAsync(Guid shipmentId, Guid userId, string userRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // Authorization check: Admin (all), Seller (own), Buyer (own purchase)
        if (userRole != "Admin")
        {
            if (userRole == "Buyer" && shipment.BuyerId != userId)
            {
                throw new UnauthorizedAccessException("You can only view insurance for your own shipments.");
            }

            if (userRole == "Seller" && shipment.SellerId != userId)
            {
                throw new UnauthorizedAccessException("You can only view insurance for your own shipments.");
            }
        }

        var insurance = await _context.InsuranceRecords
            .FirstOrDefaultAsync(i => i.ShipmentId == shipmentId);

        return insurance != null ? MapToInsuranceResponseDto(insurance) : null;
    }

    public async Task<List<TrackingEventResponseDto>> GetTrackingEventsAsync(Guid shipmentId, Guid userId, string userRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // Authorization check: Admin (all), Seller (own), Buyer (own purchase)
        if (userRole != "Admin")
        {
            if (userRole == "Buyer" && shipment.BuyerId != userId)
            {
                throw new UnauthorizedAccessException("You can only view tracking for your own shipments.");
            }

            if (userRole == "Seller" && shipment.SellerId != userId)
            {
                throw new UnauthorizedAccessException("You can only view tracking for your own shipments.");
            }
        }

        // Return timeline chronologically (oldest first)
        var events = await _context.ShipmentTrackingEvents
            .Where(e => e.ShipmentId == shipmentId)
            .OrderBy(e => e.OccurredAt)
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
            ExternalEventCode = request.ExternalEventCode,
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(trackingEvent);
        await _context.SaveChangesAsync();

        return MapToTrackingResponseDto(trackingEvent);
    }

    public async Task<CourierBookingResult> BookShipmentAsync(Guid shipmentId, Guid userId, string userRole)
    {
        // Only Admin can execute booking
        if (userRole != "Admin")
        {
            throw new UnauthorizedAccessException("Only administrators can book shipments with couriers.");
        }

        var shipment = await _context.Shipments
            .Include(s => s.ShippingPlan)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // IDEMPOTENCY CHECK #1: If already has booking references, return existing result FIRST
        // This prevents duplicate provider calls on retry/concurrent requests
        if (!string.IsNullOrEmpty(shipment.TrackingNumber) && !string.IsNullOrEmpty(shipment.ExternalShipmentReference))
        {
            return new CourierBookingResult
            {
                Success = true,
                CourierName = shipment.CourierName,
                ExternalShipmentReference = shipment.ExternalShipmentReference,
                TrackingNumber = shipment.TrackingNumber,
                SelectedService = shipment.SelectedService
            };
        }

        // PRECONDITION 1: Shipment must exist (already checked above)

        // PRECONDITION 2: ShippingPlan must exist
        if (shipment.ShippingPlan == null)
        {
            throw new InvalidOperationException("Cannot book shipment: No shipping plan has been generated.");
        }

        // PRECONDITION 3: Plan must be Admin-approved
        if (!shipment.ShippingPlan.IsApproved)
        {
            throw new InvalidOperationException("Cannot book shipment: Shipping plan has not been approved by Admin.");
        }

        // PRECONDITION 4: Shipment must be ReadyForBooking OR already Booked (for idempotency)
        // Note: Already-booked shipments are caught by IDEMPOTENCY CHECK #1 above
        if (shipment.Status != "ReadyForBooking")
        {
            throw new InvalidOperationException(
                $"Cannot book shipment: Shipment status is '{shipment.Status}', expected 'ReadyForBooking'.");
        }

        // Call courier provider adapter for simulation
        var bookingRequest = new CourierBookingRequest
        {
            ShipmentNumber = shipment.Id.ToString(),
            Origin = $"{shipment.OriginAddress}, {shipment.OriginRegion}",
            Destination = $"{shipment.DestinationAddress}, {shipment.DestinationRegion}",
            PackageDescription = shipment.PackageDescription,
            DeclaredValue = shipment.DeclaredValue,
            Currency = shipment.Currency,
            ServiceType = shipment.ShippingPlan.RecommendedServiceType,
            Weight = shipment.PackageWeight
        };

        var bookingResult = await _shippingProvider.BookShipmentAsync(bookingRequest);

        // FAILURE HANDLING: If provider fails, do not mark as booked
        if (!bookingResult.Success)
        {
            throw new InvalidOperationException($"Courier booking failed: {bookingResult.ErrorMessage}");
        }

        // SAVE RESULT: Persist booking information atomically
        shipment.CourierName = bookingResult.CourierName;
        shipment.ExternalShipmentReference = bookingResult.ExternalShipmentReference;
        shipment.TrackingNumber = bookingResult.TrackingNumber;
        shipment.SelectedService = bookingResult.SelectedService;
        shipment.BookedAt = DateTime.UtcNow;
        shipment.Status = "Booked";
        shipment.UpdatedAt = DateTime.UtcNow;

        // Create audit tracking event for booking (only on first successful booking)
        var bookingEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipment.Id,
            EventType = "Booked",
            Location = "Courier Booking",
            Description = $"Shipment booked with {bookingResult.CourierName} - Tracking: {bookingResult.TrackingNumber}",
            PerformedByUserId = userId,
            PerformedByRole = userRole,
            PreviousState = "ReadyForBooking",
            NewState = "Booked",
            Reason = $"Courier: {bookingResult.CourierName}, Service: {bookingResult.SelectedService}",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(bookingEvent);

        await _context.SaveChangesAsync();

        return bookingResult;
    }

    private void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        var validTransitions = new Dictionary<string, List<string>>
        {
            // Planning phase
            { "Pending", new List<string> { "PlanGenerated", "Cancelled", "Planning" } },
            { "Planning", new List<string> { "PlanGenerated", "Pending", "Cancelled" } },
            
            // Plan review phase
            { "PlanGenerated", new List<string> { "ReadyForBooking", "Cancelled", "Pending", "Planning" } },
            { "ReadyForBooking", new List<string> { "Booked", "Cancelled", "Pending", "Planning" } },
            
            // Operational delivery phase
            { "Booked", new List<string> { "PickedUp", "InTransit", "Cancelled" } },
            { "PickedUp", new List<string> { "InTransit", "Exception", "Cancelled" } },
            { "InTransit", new List<string> { "CustomsHold", "OutForDelivery", "Exception", "DeliveryFailed" } },
            { "CustomsHold", new List<string> { "InTransit", "Exception", "Cancelled" } },
            { "OutForDelivery", new List<string> { "Delivered", "DeliveryFailed", "Exception" } },
            
            // Terminal/exception states
            { "Delivered", new List<string>() }, // Terminal state
            { "DeliveryFailed", new List<string> { "Cancelled", "Exception" } },
            { "Exception", new List<string> { "InTransit", "Cancelled" } },
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
            ExternalShipmentReference = shipment.ExternalShipmentReference,
            SelectedService = shipment.SelectedService,
            GenerationSource = shipment.ShippingPlan?.GenerationSource,
            CreatedAt = shipment.CreatedAt,
            UpdatedAt = shipment.UpdatedAt,
            BookedAt = shipment.BookedAt,
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
            DeclaredValue = record.DeclaredValue,
            CoverageAmount = record.CoverageAmount,
            Currency = record.Currency,
            CoverageType = record.CoverageType,
            PolicyNumber = record.PolicyNumber,
            PolicyReference = record.PolicyReference,
            ProviderName = record.ProviderName,
            PremiumAmount = record.PremiumAmount,
            PolicyStartDate = record.PolicyStartDate,
            PolicyEndDate = record.PolicyEndDate,
            Status = record.Status,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
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
            ExternalEventCode = evt.ExternalEventCode,
            OccurredAt = evt.OccurredAt,
            RecordedAt = evt.RecordedAt
        };
    }
}
