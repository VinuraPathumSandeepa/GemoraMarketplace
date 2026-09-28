using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs;

public class ResendVerificationCodeDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(
        ErrorMessage = "Please enter a valid email address."
    )]
    public string Email { get; set; } = string.Empty;
}