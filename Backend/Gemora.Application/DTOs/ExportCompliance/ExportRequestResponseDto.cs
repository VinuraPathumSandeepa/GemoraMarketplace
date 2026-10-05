namespace Gemora.Application.DTOs.ExportCompliance;

public class ExportRequestResponseDto
{
    public Guid Id { get; set; }

    public string OriginCountry { get; set; } = string.Empty;

    public string DestinationCountry { get; set; } = string.Empty;

    public decimal DeclaredValue { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string? Purpose { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? SubmittedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
