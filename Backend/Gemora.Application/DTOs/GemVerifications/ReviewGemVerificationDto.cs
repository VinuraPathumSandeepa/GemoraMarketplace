using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.GemVerifications;

public class ReviewGemVerificationDto
{
    [Required(ErrorMessage = "Decision is required.")]
    public string Decision { get; set; } = string.Empty;

    [StringLength(
        2000,
        ErrorMessage = "Review notes cannot exceed 2000 characters.")]
    public string? ReviewNotes { get; set; }
}