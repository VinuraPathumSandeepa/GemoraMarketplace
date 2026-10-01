using System.Text.Json.Serialization;

namespace Gemora.Application.DTOs;

public class MyProfileDto
{
    [JsonPropertyName("userId")]
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    public bool IsEmailVerified { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}