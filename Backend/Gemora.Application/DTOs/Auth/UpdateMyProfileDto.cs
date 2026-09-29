using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs;

public class UpdateMyProfileDto
{
    [Required]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Full name must be between 2 and 100 characters."
    )]
    public string FullName { get; set; } = string.Empty;


    [StringLength(
        20,
        ErrorMessage = "Phone number cannot exceed 20 characters."
    )]
    [RegularExpression(
        @"^$|^\+[1-9]\d{6,14}$",
        ErrorMessage = "Enter the phone number in international format, for example +94771234567."
    )]
    public string? PhoneNumber { get; set; }


    [StringLength(
        2,
        ErrorMessage = "Country code cannot exceed 2 letters."
    )]
    [RegularExpression(
        @"^$|^[A-Za-z]{2}$",
        ErrorMessage = "Country code must contain 2 letters, for example LK."
    )]
    public string? CountryCode { get; set; }


    [StringLength(
        100,
        ErrorMessage = "Region cannot exceed 100 characters."
    )]
    public string? Region { get; set; }
}