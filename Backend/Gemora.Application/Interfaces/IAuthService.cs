using Gemora.Application.DTOs;

namespace Gemora.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> Register(RegisterDto dto);

    Task<AuthResult> Login(LoginDto dto);
}