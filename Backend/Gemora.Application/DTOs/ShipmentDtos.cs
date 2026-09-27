using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs;

public class CreateShipmentRequestDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [StringLength(200)]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Destination { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal DeclaredValue { get; set; }

    [StringLength(20)]
    public string Currency { get; set; } = "USD";

    [Required]
    [StringLength(500)]
    public string PackageDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SelectedService { get; set; } = string.Empty;

    [StringLength(100)]
    public string CourierName { get; set; } = string.Empty;
}

public class UpdateShipmentStatusRequestDto
{
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;
}

public class CreateInsuranceRecordRequestDto
{
    [Required]
    public Guid ShipmentId { get; set; }

    [Required]
    [StringLength(100)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string PolicyReference { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal DeclaredValue { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal CoverageAmount { get; set; }

    [StringLength(50)]
    public string CoverageType { get; set; } = "Standard";

    [Range(0, double.MaxValue)]
    public decimal PremiumAmount { get; set; }

    [StringLength(20)]
    public string Currency { get; set; } = "USD";
}

public class ShipmentResponseDto
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
    public string? TrackingNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InsuranceRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string PolicyReference { get; set; } = string.Empty;
    public decimal DeclaredValue { get; set; }
    public decimal CoverageAmount { get; set; }
    public string CoverageType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal PremiumAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

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

public class ShippingPlanResponseDto
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string RecommendedServiceType { get; set; } = string.Empty;
    public bool InsuranceRecommended { get; set; }
    public decimal RecommendedCoverage { get; set; }
    public string Requirements { get; set; } = string.Empty;
    public string Warnings { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
