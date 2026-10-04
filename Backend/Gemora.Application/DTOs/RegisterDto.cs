using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters."
    )]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(
        ErrorMessage = "Please enter a valid email address."
    )]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [StringLength(
        20,
        MinimumLength = 7,
        ErrorMessage = "Phone number must be between 7 and 20 characters."
    )]
    [RegularExpression(
        @"^\+[1-9]\d{6,14}$",
        ErrorMessage =
            "Phone number must be in international format, for example +94771234567."
    )]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required.")]
    [StringLength(
        2,
        MinimumLength = 2,
        ErrorMessage = "Country code must contain exactly 2 letters."
    )]
    [RegularExpression(
        @"^[A-Za-z]{2}$",
        ErrorMessage =
            "Country code must be a valid 2-letter code such as LK, US, GB or AU."
    )]
    public string CountryCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Region is required.")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Region must be between 2 and 100 characters."
    )]
    public string Region { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(
        6,
        ErrorMessage = "Password must contain at least 6 characters."
    )]
    [MaxLength(
        100,
        ErrorMessage = "Password cannot exceed 100 characters."
    )]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;
}