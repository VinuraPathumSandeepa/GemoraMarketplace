using System.ComponentModel.DataAnnotations;
using Gemora.Domain.Enums;

namespace Gemora.Application.DTOs;

// Shipment Creation DTO
public class CreateShipmentDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Destination { get; set; } = string.Empty;

    // These fields are derived from the Order and should not be provided by client
    // Kept for backward compatibility but ignored by service layer
    public decimal DeclaredValue { get; set; }

    [MaxLength(20)]
    public string? Currency { get; set; }

    [Required]
    [MaxLength(500)]
    public string PackageDescription { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SelectedService { get; set; }

    [MaxLength(100)]
    public string? CourierName { get; set; }
}

// Shipment Response DTO
public class ShipmentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public Guid BuyerUserId { get; set; }
    public Guid SellerUserId { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PackageDescription { get; set; } = string.Empty;
    public string SelectedService { get; set; } = string.Empty;
    public string CourierName { get; set; } = string.Empty;
    public string? ExternalShipmentReference { get; set; }
    public string? TrackingNumber { get; set; }
    public ShipmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Shipping Plan Request DTO
public class GenerateShippingPlanRequest
{
    [Required]
    public Guid ShipmentId { get; set; }
}

// Shipping Plan Response DTO
public class ShippingPlanDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string RecommendedServiceType { get; set; } = string.Empty;
    public bool InsuranceRecommended { get; set; }
    public decimal RecommendedCoverage { get; set; }
    public List<string> Requirements { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> RiskReasons { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }
}

// Admin Approval Request DTO
public class ApproveShippingPlanRequest
{
    [Required]
    public Guid ShipmentId { get; set; }
}

// Status Update Request DTO
public class UpdateShipmentStatusRequest
{
    [Required]
    public ShipmentStatus NewStatus { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

// Tracking Event DTO
public class TrackingEventDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public string? ExternalEventCode { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime RecordedAt { get; set; }
}

// Create Tracking Event Request DTO
public class CreateTrackingEventRequest
{
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string LocationText { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ExternalEventCode { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateTime OccurredAt { get; set; }
}

// Create Insurance Record Request DTO
public class CreateInsuranceRecordRequest
{
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string PolicyReference { get; set; } = string.Empty;

    [Required]
    public decimal CoverageAmount { get; set; }

    [Required]
    public InsuranceCoverageType CoverageType { get; set; }

    [Required]
    public InsuranceStatus Status { get; set; }

    [Required]
    public decimal PremiumAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string Currency { get; set; } = string.Empty;
}

// Insurance Record DTO
public class InsuranceRecordDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string PolicyReference { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public decimal CoverageAmount { get; set; }
    public InsuranceCoverageType CoverageType { get; set; }
    public InsuranceStatus Status { get; set; }
    public decimal PremiumAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// AI Agent Response Models (internal use)
public class ShippingPlanGenerationResult
{
    public bool Success { get; set; }
    public ShippingPlanDto? Plan { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

public class RiskAssessmentResult
{
    public RiskLevel RiskLevel { get; set; }
    public List<string> RiskReasons { get; set; } = new();
    public List<string> HandlingWarnings { get; set; } = new();
    public List<string> RecommendedPrecautions { get; set; } = new();
}
