using Gemora.Application.DTOs;
<<<<<<< Updated upstream
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gemora.Application.Services;

/// <summary>
/// Implements shipment business logic with comprehensive validation, authorization, and state management.
/// </summary>
public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShippingPlanRepository _shippingPlanRepository;
    private readonly IShipmentTrackingRepository _trackingRepository;
    private readonly IInsuranceRepository _insuranceRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingAgentService _agentService;
    private readonly ILogger<ShipmentService> _logger;

    public ShipmentService(
        IShipmentRepository shipmentRepository,
        IShippingPlanRepository shippingPlanRepository,
        IShipmentTrackingRepository trackingRepository,
        IInsuranceRepository insuranceRepository,
        IOrderRepository orderRepository,
        IShippingAgentService agentService,
        ILogger<ShipmentService> logger)
    {
        _shipmentRepository = shipmentRepository;
        _shippingPlanRepository = shippingPlanRepository;
        _trackingRepository = trackingRepository;
        _insuranceRepository = insuranceRepository;
        _orderRepository = orderRepository;
        _agentService = agentService;
        _logger = logger;
    }

    public async Task<ShipmentDto> CreateShipmentAsync(
        CreateShipmentDto request,
        Guid authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating shipment for order {OrderId} by user {UserId}", 
            request.OrderId, authenticatedUserId);

        // Step 1: Verify order exists
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order {request.OrderId} not found");
        }

        // Step 2: Verify order is in Paid status (eligible for shipment)
        if (order.Status != OrderStatus.Paid)
        {
            throw new InvalidOperationException(
                $"Order {request.OrderId} is not eligible for shipment. Current status: {order.Status}. Only Paid orders can create shipments.");
        }

        // Step 3: Verify authenticated user is the Seller assigned to this order
        if (order.SellerUserId != authenticatedUserId)
        {
            _logger.LogWarning("User {UserId} attempted to create shipment for order {OrderId} owned by seller {SellerUserId}",
                authenticatedUserId, request.OrderId, order.SellerUserId);
            throw new InvalidOperationException("You can only create shipments for orders you are selling");
        }

        // Step 4: Check whether an active shipment already exists for the order
        var existingShipment = await _shipmentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existingShipment != null && !IsTerminalStatus(existingShipment.Status))
        {
            throw new InvalidOperationException($"An active shipment already exists for order {request.OrderId}");
        }

        // Step 5: Validate order has valid declared value
        if (order.TotalAmount <= 0)
        {
            throw new InvalidOperationException("Order total amount must be greater than zero");
        }

        // Use order's total amount as declared value for consistency
        var declaredValue = order.TotalAmount;
        var currency = order.Currency;

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new InvalidOperationException("Order currency is not set");
        }

        // Step 6: Generate unique shipment number
        var shipmentNumber = $"SHP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20).ToUpper();

        // Step 7: Create shipment entity with data derived from order
=======
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

>>>>>>> Stashed changes
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
<<<<<<< Updated upstream
            ShipmentNumber = shipmentNumber,
            BuyerUserId = order.BuyerUserId, // Derived from order
            SellerUserId = order.SellerUserId, // Derived from order
            Origin = request.Origin,
            Destination = request.Destination,
            DeclaredValue = declaredValue, // Derived from order
            Currency = currency, // Derived from order
            PackageDescription = request.PackageDescription,
            SelectedService = string.IsNullOrWhiteSpace(request.SelectedService) ? "Standard" : request.SelectedService,
            CourierName = string.IsNullOrWhiteSpace(request.CourierName) ? "MockCourier" : request.CourierName,
            Status = ShipmentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdShipment = await _shipmentRepository.AddAsync(shipment, cancellationToken);

        _logger.LogInformation("Created shipment {ShipmentNumber} for paid order {OrderId} by seller {SellerUserId}", 
            shipmentNumber, request.OrderId, authenticatedUserId);

        return MapToDto(createdShipment);
    }

    public async Task<ShipmentDto?> GetShipmentByIdAsync(
        Guid shipmentId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByIdWithDetailsAsync(shipmentId, cancellationToken);
        
        if (shipment == null)
        {
            return null;
        }

        // Authorization: Ensure users can only access their own shipments
        if (!IsAuthorizedToView(shipment, authenticatedUserId, userRole))
        {
            _logger.LogWarning("User {UserId} with role {Role} attempted to access unauthorized shipment {ShipmentId}",
                authenticatedUserId, userRole, shipmentId);
            return null;
        }

        return MapToDto(shipment);
    }

    public async Task<ShipmentDto?> GetShipmentByOrderIdAsync(
        Guid orderId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByOrderIdAsync(orderId, cancellationToken);
        
        if (shipment == null)
        {
            return null;
        }

        // Authorization check
        if (!IsAuthorizedToView(shipment, authenticatedUserId, userRole))
        {
            return null;
        }

        return MapToDto(shipment);
    }

    public async Task<List<ShipmentDto>> GetUserShipmentsAsync(
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        List<Shipment> shipments;

        switch (userRole)
        {
            case UserRoles.Seller:
                // Sellers can see shipments for their own sales
                shipments = await _shipmentRepository.GetBySellerUserIdAsync(authenticatedUserId, cancellationToken);
                break;

            case UserRoles.Buyer:
                // Buyers can see their own shipments
                shipments = await _shipmentRepository.GetByBuyerUserIdAsync(authenticatedUserId, cancellationToken);
                break;

            case UserRoles.Admin:
                // Admins can see all shipments
                shipments = await _shipmentRepository.GetAllAsync(cancellationToken);
                break;

            default:
                // Other roles have no access
                _logger.LogWarning("Role {Role} does not have permission to view shipments", userRole);
                return new List<ShipmentDto>();
        }

        return shipments.Select(MapToDto).ToList();
    }

    public async Task<ShipmentDto> UpdateShipmentStatusAsync(
        Guid shipmentId,
        ShipmentStatus newStatus,
        Guid authenticatedUserId,
        string userRole,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken);
        
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {shipmentId} not found");
        }

        // Validate status transition
        ValidateStatusTransition(shipment.Status, newStatus, userRole);

        // Authorization: Only allow appropriate roles to update status
        if (userRole == UserRoles.Buyer)
        {
            throw new InvalidOperationException("Buyers cannot change shipment status");
        }

        if (userRole == UserRoles.Seller && shipment.SellerUserId != authenticatedUserId)
        {
            throw new InvalidOperationException("You can only update status for your own shipments");
        }

        if (userRole != UserRoles.Admin && userRole != UserRoles.Seller)
        {
            throw new InvalidOperationException("Only an admin or the owning seller can update shipment status");
        }

        // Prevent sellers from setting arbitrary final delivery states
        if (userRole == UserRoles.Seller && IsFinalDeliveryStatus(newStatus))
        {
            throw new InvalidOperationException("Sellers cannot set final delivery status");
        }

        // Update status
        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTime.UtcNow;

        await _shipmentRepository.UpdateAsync(shipment, cancellationToken);
=======
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
            EventTimestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
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
>>>>>>> Stashed changes

        // Add tracking event for status change
        var trackingEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
<<<<<<< Updated upstream
            ShipmentId = shipmentId,
            Status = newStatus.ToString(),
            LocationText = "System",
            Description = $"Status changed to {newStatus}" + (reason != null ? $": {reason}" : ""),
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        await _trackingRepository.AddAsync(trackingEvent, cancellationToken);

        _logger.LogInformation("Updated shipment {ShipmentId} status from {OldStatus} to {NewStatus} by user {UserId}",
            shipmentId, shipment.Status, newStatus, authenticatedUserId);

        return MapToDto(shipment);
    }

    public async Task<ShippingPlanDto> ApproveShippingPlanAsync(
        Guid shipmentId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin {AdminId} approving shipping plan for shipment {ShipmentId}",
            adminUserId, shipmentId);

        // Verify the shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken);
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {shipmentId} not found");
        }

        // Verify the associated shipping plan exists
        var plan = await _shippingPlanRepository.GetByShipmentIdAsync(shipmentId, cancellationToken);
        if (plan == null)
        {
            throw new InvalidOperationException($"No shipping plan found for shipment {shipmentId}");
        }

        // Verify the plan is awaiting admin approval
        if (plan.Status != "PendingAdminApproval")
        {
            throw new InvalidOperationException($"Plan is not pending approval. Current status: {plan.Status}");
        }

        // Validate the stored plan again against current deterministic business rules
        var validationErrors = ValidatePlanForApproval(plan, shipment);
        if (validationErrors.Any())
        {
            throw new InvalidOperationException(
                $"Plan validation failed: {string.Join("; ", validationErrors)}");
        }

        // Record the approval
        plan.Status = "Approved";
        plan.ApprovedAt = DateTime.UtcNow;
        plan.ApprovedByUserId = adminUserId;

        await _shippingPlanRepository.UpdateAsync(plan, cancellationToken);

        _logger.LogInformation("Shipping plan {PlanId} approved for shipment {ShipmentId} by admin {AdminId}",
            plan.Id, shipmentId, adminUserId);

        return MapPlanToDto(plan);
    }

    public async Task<ShippingPlanDto?> GetShippingPlanAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        var plan = await _shippingPlanRepository.GetByShipmentIdAsync(shipmentId, cancellationToken);
        return plan != null ? MapPlanToDto(plan) : null;
    }

    public async Task<ShippingPlanGenerationResult> GenerateShippingPlanAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        return await _agentService.GenerateShippingPlanAsync(shipmentId, cancellationToken);
    }

    public async Task<List<TrackingEventDto>> GetShipmentTrackingEventsAsync(
        Guid shipmentId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        // First verify user has access to this shipment
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken);
        if (shipment == null)
        {
            return new List<TrackingEventDto>();
        }

        if (!IsAuthorizedToView(shipment, authenticatedUserId, userRole))
        {
            _logger.LogWarning("User {UserId} with role {Role} attempted to access unauthorized tracking events for shipment {ShipmentId}",
                authenticatedUserId, userRole, shipmentId);
            return new List<TrackingEventDto>();
        }

        // Retrieve tracking events
        var events = await _trackingRepository.GetByShipmentIdAsync(shipmentId, cancellationToken);
        return events.Select(e => new TrackingEventDto
        {
            Id = e.Id,
            ShipmentId = e.ShipmentId,
            Status = e.Status,
            LocationText = e.LocationText,
            Description = e.Description,
            OccurredAt = e.OccurredAt,
            RecordedAt = e.RecordedAt
        }).OrderByDescending(e => e.OccurredAt).ToList();
    }

    public async Task<List<InsuranceRecordDto>> GetShipmentInsuranceRecordsAsync(
        Guid shipmentId,
        Guid authenticatedUserId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        // First verify user has access to this shipment
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken);
        if (shipment == null)
        {
            return new List<InsuranceRecordDto>();
        }

        if (!IsAuthorizedToView(shipment, authenticatedUserId, userRole))
        {
            _logger.LogWarning("User {UserId} with role {Role} attempted to access unauthorized insurance records for shipment {ShipmentId}",
                authenticatedUserId, userRole, shipmentId);
            return new List<InsuranceRecordDto>();
        }

        // Retrieve insurance records
        var records = await _insuranceRepository.GetByShipmentIdAsync(shipmentId, cancellationToken);
        return records.Select(r => new InsuranceRecordDto
        {
            Id = r.Id,
            ShipmentId = r.ShipmentId,
            PolicyReference = r.PolicyReference,
            Provider = r.Provider,
            CoverageAmount = r.CoverageAmount,
            Currency = r.Currency,
            CoverageType = r.CoverageType,
            PremiumAmount = r.PremiumAmount,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }

    public async Task<TrackingEventDto> AddTrackingEventAsync(
        Guid shipmentId,
        CreateTrackingEventRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin {AdminId} adding tracking event for shipment {ShipmentId}",
            adminUserId, shipmentId);

        // Verify the shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken);
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {shipmentId} not found");
        }

        // Validate status value
        if (!Enum.TryParse<ShipmentStatus>(request.Status, out var parsedStatus))
        {
            throw new InvalidOperationException($"Invalid status value: {request.Status}");
        }

        // Validate occurredAt is not in the future
        if (request.OccurredAt > DateTime.UtcNow.AddHours(1))
        {
            throw new InvalidOperationException("Event occurrence time cannot be in the future");
        }

        // Create tracking event
        var trackingEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            Status = request.Status,
            LocationText = request.LocationText,
            ExternalEventCode = request.ExternalEventCode,
            Description = request.Description,
            OccurredAt = request.OccurredAt,
            RecordedAt = DateTime.UtcNow
        };

        await _trackingRepository.AddAsync(trackingEvent, cancellationToken);

        _logger.LogInformation("Added tracking event {EventId} for shipment {ShipmentId} by admin {AdminId}",
            trackingEvent.Id, shipmentId, adminUserId);

        return new TrackingEventDto
        {
            Id = trackingEvent.Id,
            ShipmentId = trackingEvent.ShipmentId,
            Status = trackingEvent.Status,
            LocationText = trackingEvent.LocationText,
            ExternalEventCode = trackingEvent.ExternalEventCode,
            Description = trackingEvent.Description,
            OccurredAt = trackingEvent.OccurredAt,
            RecordedAt = trackingEvent.RecordedAt
        };
    }

    public async Task<InsuranceRecordDto> CreateInsuranceRecordAsync(
        Guid shipmentId,
        CreateInsuranceRecordRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin {AdminId} creating insurance record for shipment {ShipmentId}",
            adminUserId, shipmentId);

        // Verify the shipment exists
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken);
        if (shipment == null)
        {
            throw new InvalidOperationException($"Shipment {shipmentId} not found");
        }

        // Validate coverage amount
        if (request.CoverageAmount <= 0)
        {
            throw new InvalidOperationException("Coverage amount must be greater than zero");
        }

        if (request.PremiumAmount < 0)
        {
            throw new InvalidOperationException("Premium amount cannot be negative");
        }

        // Check for duplicate policy reference
        var existingRecords = await _insuranceRepository.GetByShipmentIdAsync(shipmentId, cancellationToken);
        if (existingRecords.Any(r => r.PolicyReference == request.PolicyReference))
        {
            throw new InvalidOperationException($"Insurance record with policy reference {request.PolicyReference} already exists");
        }

        // Create insurance record (simulated/academic only)
=======
            ShipmentId = shipment.Id,
            EventType = $"StatusChanged:{oldStatus}->{request.Status}",
            Location = "System",
            Description = request.Notes ?? $"Status changed from {oldStatus} to {request.Status}",
            EventTimestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
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

>>>>>>> Stashed changes
        var insuranceRecord = new InsuranceRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
<<<<<<< Updated upstream
            Provider = request.Provider,
            PolicyReference = request.PolicyReference,
            DeclaredValue = shipment.DeclaredValue,
            CoverageAmount = request.CoverageAmount,
            CoverageType = request.CoverageType,
            Status = request.Status,
            PremiumAmount = request.PremiumAmount,
            Currency = request.Currency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _insuranceRepository.AddAsync(insuranceRecord, cancellationToken);

        _logger.LogInformation("Created insurance record {PolicyReference} for shipment {ShipmentId} by admin {AdminId}",
            request.PolicyReference, shipmentId, adminUserId);

        return new InsuranceRecordDto
        {
            Id = insuranceRecord.Id,
            ShipmentId = insuranceRecord.ShipmentId,
            Provider = insuranceRecord.Provider,
            PolicyReference = insuranceRecord.PolicyReference,
            DeclaredValue = insuranceRecord.DeclaredValue,
            CoverageAmount = insuranceRecord.CoverageAmount,
            CoverageType = insuranceRecord.CoverageType,
            Status = insuranceRecord.Status,
            PremiumAmount = insuranceRecord.PremiumAmount,
            Currency = insuranceRecord.Currency,
            CreatedAt = insuranceRecord.CreatedAt,
            UpdatedAt = insuranceRecord.UpdatedAt
        };
    }

    #region Helper Methods

    private static bool IsTerminalStatus(ShipmentStatus status)
    {
        return status is ShipmentStatus.Delivered or 
                          ShipmentStatus.Cancelled or 
                          ShipmentStatus.DeliveryFailed;
    }

    private static bool IsFinalDeliveryStatus(ShipmentStatus status)
    {
        return status is ShipmentStatus.Delivered or 
                            ShipmentStatus.DeliveryFailed;
    }

    private static bool IsAuthorizedToView(Shipment shipment, Guid userId, string userRole)
    {
        return userRole switch
        {
            UserRoles.Admin => true,
            UserRoles.Seller => shipment.SellerUserId == userId,
            UserRoles.Buyer => shipment.BuyerUserId == userId,
            _ => false
        };
    }

    private static void ValidateStatusTransition(ShipmentStatus currentStatus, ShipmentStatus newStatus, string userRole)
    {
        // Define valid transitions
        var validTransitions = new Dictionary<ShipmentStatus, List<ShipmentStatus>>
        {
            { ShipmentStatus.Pending, new List<ShipmentStatus> { ShipmentStatus.Planning, ShipmentStatus.Cancelled } },
            { ShipmentStatus.Planning, new List<ShipmentStatus> { ShipmentStatus.ReadyForBooking, ShipmentStatus.Cancelled } },
            { ShipmentStatus.ReadyForBooking, new List<ShipmentStatus> { ShipmentStatus.Booked, ShipmentStatus.Cancelled } },
            { ShipmentStatus.Booked, new List<ShipmentStatus> { ShipmentStatus.PickedUp, ShipmentStatus.Cancelled } },
            { ShipmentStatus.PickedUp, new List<ShipmentStatus> { ShipmentStatus.InTransit } },
            { ShipmentStatus.InTransit, new List<ShipmentStatus> { ShipmentStatus.CustomsHold, ShipmentStatus.OutForDelivery, ShipmentStatus.Exception } },
            { ShipmentStatus.CustomsHold, new List<ShipmentStatus> { ShipmentStatus.InTransit, ShipmentStatus.Exception } },
            { ShipmentStatus.OutForDelivery, new List<ShipmentStatus> { ShipmentStatus.Delivered, ShipmentStatus.DeliveryFailed } },
            { ShipmentStatus.Exception, new List<ShipmentStatus> { ShipmentStatus.InTransit, ShipmentStatus.Cancelled } }
        };

        // Check if transition is valid
        if (validTransitions.TryGetValue(currentStatus, out var allowedNextStates))
        {
            if (!allowedNextStates.Contains(newStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot transition from {currentStatus} to {newStatus}. Allowed transitions: {string.Join(", ", allowedNextStates)}");
            }
        }
        else
        {
            throw new InvalidOperationException($"No transitions defined from status {currentStatus}");
        }

        // Prevent invalid transitions mentioned in requirements
        if (currentStatus == ShipmentStatus.Pending && newStatus == ShipmentStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot move directly from Pending to Delivered");
        }
    }

    private static List<string> ValidatePlanForApproval(ShippingPlan plan, Shipment shipment)
    {
        var errors = new List<string>();

        // Validate risk level
        if (!Enum.IsDefined(typeof(RiskLevel), plan.RiskLevel))
        {
            errors.Add($"Invalid risk level: {plan.RiskLevel}");
        }

        // Validate recommended coverage
        if (plan.RecommendedCoverage <= 0)
        {
            errors.Add("Recommended coverage must be greater than zero");
        }

        if (plan.InsuranceRecommended && plan.RecommendedCoverage < shipment.DeclaredValue)
        {
            errors.Add("Insurance coverage should not be less than declared value when insurance is recommended");
        }

        // Validate service type is not empty
        if (string.IsNullOrWhiteSpace(plan.RecommendedServiceType))
        {
            errors.Add("Recommended service type is required");
        }

        // Validate shipment IDs match
        if (plan.ShipmentId != shipment.Id)
        {
            errors.Add("Plan shipment ID does not match shipment");
        }

        return errors;
    }

    private static ShipmentDto MapToDto(Shipment shipment)
    {
        return new ShipmentDto
        {
            Id = shipment.Id,
            OrderId = shipment.OrderId,
            ShipmentNumber = shipment.ShipmentNumber,
            BuyerUserId = shipment.BuyerUserId,
            SellerUserId = shipment.SellerUserId,
            Origin = shipment.Origin,
            Destination = shipment.Destination,
            DeclaredValue = shipment.DeclaredValue,
            Currency = shipment.Currency,
            PackageDescription = shipment.PackageDescription,
            SelectedService = shipment.SelectedService,
            CourierName = shipment.CourierName,
            ExternalShipmentReference = shipment.ExternalShipmentReference,
            TrackingNumber = shipment.TrackingNumber,
            Status = shipment.Status,
            CreatedAt = shipment.CreatedAt,
            UpdatedAt = shipment.UpdatedAt
        };
    }

    private static ShippingPlanDto MapPlanToDto(ShippingPlan plan)
    {
        var requirements = System.Text.Json.JsonSerializer.Deserialize<List<string>>(plan.Requirements) ?? new List<string>();
        var warnings = System.Text.Json.JsonSerializer.Deserialize<List<string>>(plan.Warnings) ?? new List<string>();

        return new ShippingPlanDto
        {
            Id = plan.Id,
            ShipmentId = plan.ShipmentId,
            RiskLevel = plan.RiskLevel,
            RecommendedServiceType = plan.RecommendedServiceType,
            InsuranceRecommended = plan.InsuranceRecommended,
            RecommendedCoverage = plan.RecommendedCoverage,
            Requirements = requirements,
            Warnings = warnings,
            GeneratedAt = plan.GeneratedAt,
            ApprovalStatus = plan.Status,
            ApprovedAt = plan.ApprovedAt,
            ApprovedByUserId = plan.ApprovedByUserId,
            RejectionReason = plan.RejectionReason
        };
    }

    #endregion
=======
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
            .OrderByDescending(e => e.EventTimestamp)
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
            EventTimestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
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
            EventTimestamp = evt.EventTimestamp,
            CreatedAt = evt.CreatedAt
        };
    }
>>>>>>> Stashed changes
}
