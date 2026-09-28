using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs;

public class VerifyEmailDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(
        ErrorMessage = "Please enter a valid email address."
    )]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Verification code is required.")]
    [RegularExpression(
        @"^\d{6}$",
        ErrorMessage = "Verification code must contain exactly 6 digits."
    )]
    public string Code { get; set; } = string.Empty;
}