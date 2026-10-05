using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs;

public class CreateShipmentDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [StringLength(500)]
    public string OriginAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string OriginRegion { get; set; } = string.Empty;

    [Required]
    [StringLength(2)]
    public string OriginCountryCode { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string DestinationAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DestinationRegion { get; set; } = string.Empty;

    [Required]
    [StringLength(2)]
    public string DestinationCountryCode { get; set; } = string.Empty;

    public decimal? DeclaredValue { get; set; }

    [StringLength(20)]
    public string? Currency { get; set; }

    [Required]
    [StringLength(2000)]
    public string PackageDescription { get; set; } = string.Empty;

    public decimal? PackageWeight { get; set; }

    [StringLength(200)]
    public string? PackageDimensions { get; set; }

    [StringLength(100)]
    public string? PreferredService { get; set; }

    [StringLength(2000)]
    public string SpecialHandlingNotes { get; set; } = string.Empty;

    public bool ExportRequired { get; set; }
}

public class ShipmentResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public string OriginAddress { get; set; } = string.Empty;
    public string OriginRegion { get; set; } = string.Empty;
    public string OriginCountryCode { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string DestinationRegion { get; set; } = string.Empty;
    public string DestinationCountryCode { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = "USD";
    public string PackageDescription { get; set; } = string.Empty;
    public decimal? PackageWeight { get; set; }
    public string? PackageDimensions { get; set; }
    public string SpecialHandlingNotes { get; set; } = string.Empty;
    public string PreferredService { get; set; } = string.Empty;
    public bool ExportRequired { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RiskLevel { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CourierName { get; set; }
    public string? ExternalShipmentReference { get; set; }
    public string? SelectedService { get; set; }
    public string? GenerationSource { get; set; } // AI or FallbackRules
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? BookedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class UpdateShipmentStatusDto
{
    [Required]
    [RegularExpression(@"^(Pending|Planning|PlanGenerated|ReadyForBooking|Booked|PickedUp|InTransit|CustomsHold|OutForDelivery|Delivered|DeliveryFailed|Cancelled|Exception)$")]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Location { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class CreateInsuranceRecordRequest
{
    [Required]
    public Guid ShipmentId { get; set; }

    [Required]
    public decimal DeclaredValue { get; set; }

    [Required]
    public decimal CoverageAmount { get; set; }

    [StringLength(20)]
    public string? Currency { get; set; }

    [StringLength(50)]
    public string? CoverageType { get; set; }

    [StringLength(200)]
    public string? ProviderName { get; set; }
}

public class InsuranceRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public decimal DeclaredValue { get; set; }
    public decimal CoverageAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CoverageType { get; set; } = "Standard";
    public string? PolicyNumber { get; set; }
    public string? PolicyReference { get; set; }
    public string? ProviderName { get; set; }
    public decimal PremiumAmount { get; set; }
    public DateTime? PolicyStartDate { get; set; }
    public DateTime? PolicyEndDate { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AddTrackingEventDto
{
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ExternalEventCode { get; set; }
}

public class TrackingEventResponseDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExternalEventCode { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class ShippingPlanResponseDto
{
    public bool IsPreview { get; set; }
    public string? GenerationSource { get; set; }
    public string? ExecutionSummary { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string RiskLevel { get; set; } = "Medium";
    public string? RiskReasons { get; set; }
    public string RecommendedServiceType { get; set; } = "Standard";
    public bool InsuranceRecommended { get; set; }
    public decimal? RecommendedCoverageAmount { get; set; }
    public string? HandlingRequirements { get; set; }
    public string? RequiredDocuments { get; set; }
    public string? Warnings { get; set; }
    public bool IsApproved { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApprovePlanRequest
{
    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class RejectPlanRequest
{
    [StringLength(2000)]
    public string? Reason { get; set; }
}

public class RequestRevisionPlanRequest
{
    [StringLength(2000)]
    public string? Notes { get; set; }
}
