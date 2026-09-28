using Gemora.Application.DTOs;

namespace Gemora.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> Register(RegisterDto dto);

    Task<AuthResult> Login(LoginDto dto);

    Task<AuthResult> VerifyEmail(VerifyEmailDto dto);

    Task<AuthResult> ResendVerificationCode(
        ResendVerificationCodeDto dto
    );
}