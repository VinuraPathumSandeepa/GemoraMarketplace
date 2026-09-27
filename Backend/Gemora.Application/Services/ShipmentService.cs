using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class ShipmentService : IShipmentService
{
    private readonly ApplicationDbContext _context;

    public ShipmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShipmentResponseDto> CreateShipmentAsync(
        CreateShipmentRequestDto dto,
        Guid buyerUserId,
        Guid sellerUserId)
    {
        if (dto.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException("Order ID is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Origin))
        {
            throw new InvalidOperationException("Origin is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Destination))
        {
            throw new InvalidOperationException("Destination is required.");
        }

        if (dto.DeclaredValue <= 0)
        {
            throw new InvalidOperationException("Declared value must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(dto.PackageDescription))
        {
            throw new InvalidOperationException("Package description is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.SelectedService))
        {
            throw new InvalidOperationException("Selected service is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Currency))
        {
            dto.Currency = "USD";
        }

        var shipment = new Shipment
        {
            OrderId = dto.OrderId,
            ShipmentNumber = $"SHIP-{DateTime.UtcNow:yyyyMMddHHmmss}",
            BuyerUserId = buyerUserId,
            SellerUserId = sellerUserId,
            Origin = dto.Origin.Trim(),
            Destination = dto.Destination.Trim(),
            DeclaredValue = dto.DeclaredValue,
            Currency = dto.Currency.Trim(),
            PackageDescription = dto.PackageDescription.Trim(),
            SelectedService = dto.SelectedService.Trim(),
            CourierName = dto.CourierName.Trim(),
            Status = ShipmentStatus.Pending,
            TrackingNumber = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();

        var trackingEvent = new ShipmentTrackingEvent
        {
            ShipmentId = shipment.Id,
            Status = ShipmentStatus.Pending,
            LocationText = "Shipment created",
            Description = "Shipment request created and pending planning.",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(trackingEvent);
        await _context.SaveChangesAsync();

        return MapToResponse(shipment);
    }

    public async Task<ShipmentResponseDto?> GetShipmentByIdAsync(Guid shipmentId, Guid currentUserId, string currentUserRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            return null;
        }

        if (!CanAccessShipment(shipment, currentUserId, currentUserRole))
        {
            throw new UnauthorizedAccessException("You are not allowed to view this shipment.");
        }

        return MapToResponse(shipment);
    }

    public async Task<List<ShipmentResponseDto>> GetMyShipmentsAsync(Guid currentUserId, string currentUserRole)
    {
        var query = _context.Shipments.AsQueryable();

        if (currentUserRole.Equals(UserRoles.Buyer, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.BuyerUserId == currentUserId);
        }
        else if (currentUserRole.Equals(UserRoles.Seller, StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.SellerUserId == currentUserId);
        }

        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return shipments.Select(MapToResponse).ToList();
    }

    public async Task<ShipmentResponseDto> UpdateShipmentStatusAsync(
        Guid shipmentId,
        UpdateShipmentStatusRequestDto dto,
        Guid currentUserId,
        string currentUserRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new KeyNotFoundException("Shipment not found.");
        }

        if (!CanAccessShipment(shipment, currentUserId, currentUserRole) &&
            !currentUserRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("You are not allowed to update this shipment.");
        }

        var newStatus = dto.Status.Trim();
        if (string.IsNullOrWhiteSpace(newStatus))
        {
            throw new InvalidOperationException("Status is required.");
        }

        if (!IsValidStatusTransition(shipment.Status, newStatus))
        {
            throw new InvalidOperationException($"Invalid shipment status transition from '{shipment.Status}' to '{newStatus}'.");
        }

        shipment.Status = newStatus;
        shipment.UpdatedAt = DateTime.UtcNow;

        var trackingEvent = new ShipmentTrackingEvent
        {
            ShipmentId = shipment.Id,
            Status = newStatus,
            LocationText = shipment.Destination,
            Description = $"Status updated to {newStatus}.",
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(trackingEvent);
        await _context.SaveChangesAsync();

        return MapToResponse(shipment);
    }

    public async Task<InsuranceRecordResponseDto> CreateInsuranceRecordAsync(
        CreateInsuranceRecordRequestDto dto,
        Guid currentUserId,
        string currentUserRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == dto.ShipmentId);

        if (shipment == null)
        {
            throw new KeyNotFoundException("Shipment not found.");
        }

        if (!CanAccessShipment(shipment, currentUserId, currentUserRole) &&
            !currentUserRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("You are not allowed to add insurance for this shipment.");
        }

        if (dto.CoverageAmount <= 0)
        {
            throw new InvalidOperationException("Coverage amount must be greater than zero.");
        }

        var record = new InsuranceRecord
        {
            ShipmentId = shipment.Id,
            Provider = dto.Provider.Trim(),
            PolicyReference = dto.PolicyReference.Trim(),
            DeclaredValue = dto.DeclaredValue,
            CoverageAmount = dto.CoverageAmount,
            CoverageType = dto.CoverageType.Trim(),
            Status = "Active",
            PremiumAmount = dto.PremiumAmount,
            Currency = dto.Currency.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.InsuranceRecords.Add(record);
        await _context.SaveChangesAsync();

        return new InsuranceRecordResponseDto
        {
            Id = record.Id,
            ShipmentId = record.ShipmentId,
            Provider = record.Provider,
            PolicyReference = record.PolicyReference,
            DeclaredValue = record.DeclaredValue,
            CoverageAmount = record.CoverageAmount,
            CoverageType = record.CoverageType,
            Status = record.Status,
            PremiumAmount = record.PremiumAmount,
            Currency = record.Currency,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    public async Task<List<InsuranceRecordResponseDto>> GetInsuranceRecordsAsync(
        Guid shipmentId,
        Guid currentUserId,
        string currentUserRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new KeyNotFoundException("Shipment not found.");
        }

        if (!CanAccessShipment(shipment, currentUserId, currentUserRole) &&
            !currentUserRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("You are not allowed to view insurance for this shipment.");
        }

        var records = await _context.InsuranceRecords
            .Where(i => i.ShipmentId == shipmentId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return records.Select(r => new InsuranceRecordResponseDto
        {
            Id = r.Id,
            ShipmentId = r.ShipmentId,
            Provider = r.Provider,
            PolicyReference = r.PolicyReference,
            DeclaredValue = r.DeclaredValue,
            CoverageAmount = r.CoverageAmount,
            CoverageType = r.CoverageType,
            Status = r.Status,
            PremiumAmount = r.PremiumAmount,
            Currency = r.Currency,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();
    }

    public async Task<List<TrackingEventDto>> GetTrackingEventsAsync(
        Guid shipmentId,
        Guid currentUserId,
        string currentUserRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new KeyNotFoundException("Shipment not found.");
        }

        if (!CanAccessShipment(shipment, currentUserId, currentUserRole) &&
            !currentUserRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("You are not allowed to view shipment tracking.");
        }

        var events = await _context.ShipmentTrackingEvents
            .Where(e => e.ShipmentId == shipmentId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync();

        return events.Select(e => new TrackingEventDto
        {
            Id = e.Id,
            ShipmentId = e.ShipmentId,
            Status = e.Status,
            LocationText = e.LocationText,
            ExternalEventCode = e.ExternalEventCode,
            Description = e.Description,
            OccurredAt = e.OccurredAt,
            RecordedAt = e.RecordedAt
        }).ToList();
    }

    public async Task<ShippingPlanResponseDto> CreateShippingPlanAsync(
        Guid shipmentId,
        Guid currentUserId,
        string currentUserRole)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new KeyNotFoundException("Shipment not found.");
        }

        if (!CanAccessShipment(shipment, currentUserId, currentUserRole) &&
            !currentUserRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("You are not allowed to generate a shipping plan for this shipment.");
        }

        var riskLevel = shipment.DeclaredValue switch
        {
            > 20000m => "high",
            > 5000m => "medium",
            _ => "low"
        };

        var recommendedService = shipment.DeclaredValue > 10000m ? "Priority Air Freight" : "Standard Courier";
        var insuranceRecommended = shipment.DeclaredValue > 5000m || riskLevel != "low";
        var recommendedCoverage = insuranceRecommended ? shipment.DeclaredValue * 1.2m : 0m;
        var requirements = "Validate destination compliance, confirm declared value, and ensure packaging documentation is complete.";
        var warnings = shipment.DeclaredValue > 20000m
            ? "High-value item requires enhanced handling and insurance review."
            : "Standard handling plan available.";

        var existingPlan = await _context.ShippingPlans
            .FirstOrDefaultAsync(p => p.ShipmentId == shipment.Id);

        if (existingPlan == null)
        {
            existingPlan = new ShippingPlan
            {
                ShipmentId = shipment.Id,
                RiskLevel = riskLevel,
                RecommendedServiceType = recommendedService,
                InsuranceRecommended = insuranceRecommended,
                RecommendedCoverage = recommendedCoverage,
                Requirements = requirements,
                Warnings = warnings,
                GeneratedAt = DateTime.UtcNow
            };

            _context.ShippingPlans.Add(existingPlan);
        }
        else
        {
            existingPlan.RiskLevel = riskLevel;
            existingPlan.RecommendedServiceType = recommendedService;
            existingPlan.InsuranceRecommended = insuranceRecommended;
            existingPlan.RecommendedCoverage = recommendedCoverage;
            existingPlan.Requirements = requirements;
            existingPlan.Warnings = warnings;
            existingPlan.GeneratedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new ShippingPlanResponseDto
        {
            Id = existingPlan.Id,
            ShipmentId = existingPlan.ShipmentId,
            RiskLevel = existingPlan.RiskLevel,
            RecommendedServiceType = existingPlan.RecommendedServiceType,
            InsuranceRecommended = existingPlan.InsuranceRecommended,
            RecommendedCoverage = existingPlan.RecommendedCoverage,
            Requirements = existingPlan.Requirements,
            Warnings = existingPlan.Warnings,
            GeneratedAt = existingPlan.GeneratedAt
        };
    }

    private static bool IsValidStatusTransition(string currentStatus, string nextStatus)
    {
        if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(nextStatus))
        {
            return false;
        }

        if (string.Equals(currentStatus, nextStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var allowedTransitions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [ShipmentStatus.Pending] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.Planning,
                ShipmentStatus.Cancelled
            },
            [ShipmentStatus.Planning] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.ReadyForBooking,
                ShipmentStatus.Cancelled,
                ShipmentStatus.Exception
            },
            [ShipmentStatus.ReadyForBooking] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.Booked,
                ShipmentStatus.Cancelled,
                ShipmentStatus.Exception
            },
            [ShipmentStatus.Booked] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.PickedUp,
                ShipmentStatus.Cancelled,
                ShipmentStatus.Exception
            },
            [ShipmentStatus.PickedUp] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.InTransit,
                ShipmentStatus.Cancelled,
                ShipmentStatus.Exception
            },
            [ShipmentStatus.InTransit] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.CustomsHold,
                ShipmentStatus.OutForDelivery,
                ShipmentStatus.Delivered,
                ShipmentStatus.DeliveryFailed,
                ShipmentStatus.Exception,
                ShipmentStatus.Cancelled
            },
            [ShipmentStatus.CustomsHold] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.InTransit,
                ShipmentStatus.Cancelled,
                ShipmentStatus.Exception
            },
            [ShipmentStatus.OutForDelivery] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.Delivered,
                ShipmentStatus.DeliveryFailed,
                ShipmentStatus.Exception,
                ShipmentStatus.Cancelled
            },
            [ShipmentStatus.Delivered] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.Delivered
            },
            [ShipmentStatus.DeliveryFailed] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.Exception,
                ShipmentStatus.Cancelled,
                ShipmentStatus.OutForDelivery
            },
            [ShipmentStatus.Exception] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.InTransit,
                ShipmentStatus.Cancelled,
                ShipmentStatus.OutForDelivery,
                ShipmentStatus.Planning
            },
            [ShipmentStatus.Cancelled] = new(StringComparer.OrdinalIgnoreCase)
            {
                ShipmentStatus.Cancelled
            }
        };

        return allowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(nextStatus);
    }

    private static bool CanAccessShipment(Shipment shipment, Guid currentUserId, string currentUserRole)
    {
        if (currentUserRole.Equals(UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (currentUserRole.Equals(UserRoles.Buyer, StringComparison.OrdinalIgnoreCase))
        {
            return shipment.BuyerUserId == currentUserId;
        }

        if (currentUserRole.Equals(UserRoles.Seller, StringComparison.OrdinalIgnoreCase))
        {
            return shipment.SellerUserId == currentUserId;
        }

        return false;
    }

    private static ShipmentResponseDto MapToResponse(Shipment shipment)
    {
        return new ShipmentResponseDto
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
            TrackingNumber = shipment.TrackingNumber,
            Status = shipment.Status,
            CreatedAt = shipment.CreatedAt,
            UpdatedAt = shipment.UpdatedAt
        };
    }
}
