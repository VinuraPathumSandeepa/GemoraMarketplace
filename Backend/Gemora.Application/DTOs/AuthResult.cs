namespace Gemora.Application.DTOs;

public class AuthResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Token { get; set; }

    public string? ErrorCode { get; set; }
}