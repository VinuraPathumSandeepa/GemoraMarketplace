namespace Gemora.Application.DTOs.ExportCompliance;

public class BuyerExportTrackingDto
{
    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public int? GemListingId { get; set; }

    public string GemTitle { get; set; } = string.Empty;

    public string? GemImageUrl { get; set; }

    public Guid? ExportRequestId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StageDescription { get; set; } = string.Empty;

    public string OriginCountry { get; set; } = string.Empty;

    public string DestinationCountry { get; set; } = string.Empty;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsApproved { get; set; }

    public bool RequiresExport { get; set; }
}
