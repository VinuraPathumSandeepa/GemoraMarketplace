using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.ExportCompliance;

public class ExportDecisionDto
{
    [Required(ErrorMessage = "Decision is required.")]
    public string Decision { get; set; } = string.Empty;

    [StringLength(
        1000,
        ErrorMessage = "Review notes cannot exceed 1000 characters."
    )]
    public string? ReviewNotes { get; set; }
}
