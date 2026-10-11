using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.ExportCompliance;

public class CreateExportRequestDto
{
    [Required(ErrorMessage = "Origin country is required.")]
    [StringLength(100, ErrorMessage = "Origin country cannot exceed 100 characters.")]
    public string OriginCountry { get; set; } = string.Empty;

    [Required(ErrorMessage = "Destination country is required.")]
    [StringLength(100, ErrorMessage = "Destination country cannot exceed 100 characters.")]
    public string DestinationCountry { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Declared value must be greater than 0.")]
    public decimal DeclaredValue { get; set; }

    [Required(ErrorMessage = "Currency is required.")]
    [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters.")]
    public string Currency { get; set; } = "USD";

    [StringLength(500, ErrorMessage = "Purpose cannot exceed 500 characters.")]
    public string? Purpose { get; set; }
}
