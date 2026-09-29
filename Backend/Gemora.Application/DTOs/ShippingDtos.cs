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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public class UpdateShipmentStatusDto
{
    [Required]
    [RegularExpression(@"^(Pending|PlanGenerated|PlanApproved|InTransit|Delivered|Cancelled|Exception)$")]
    public string Status { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class CreateInsuranceRecordRequest
{
    [Required]
    public Guid ShipmentId { get; set; }
    
    [Required]
    public decimal CoverageAmount { get; set; }
    
    [StringLength(20)]
    public string? Currency { get; set; }
    
    [StringLength(50)]
    public string? CoverageType { get; set; }
}

public class InsuranceRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public decimal CoverageAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CoverageType { get; set; } = "Standard";
    public string? PolicyNumber { get; set; }
    public string? ProviderName { get; set; }
    public DateTime? PolicyStartDate { get; set; }
    public DateTime? PolicyEndDate { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
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
}

public class TrackingEventResponseDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ShippingPlanResponseDto
{
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
    public DateTime CreatedAt { get; set; }
}
