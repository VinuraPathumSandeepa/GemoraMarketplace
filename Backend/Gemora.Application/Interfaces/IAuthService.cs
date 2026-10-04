using Gemora.Application.DTOs;

namespace Gemora.Application.Interfaces;

public interface IAuthService
{
    // ============================================================
    // AUTHENTICATION
    // ============================================================

    Task<AuthResult> Register(
        RegisterDto dto
    );

    Task<AuthResult> Login(
        LoginDto dto
    );

    Task<AuthResult> VerifyEmail(
        VerifyEmailDto dto
    );

    Task<AuthResult> ResendVerificationCode(
        ResendVerificationCodeDto dto
    );


    // ============================================================
    // CURRENT USER PROFILE
    // ============================================================

    Task<MyProfileDto?> GetMyProfile(
        Guid userId
    );

    Task<MyProfileDto?> UpdateMyProfile(
        Guid userId,
        UpdateMyProfileDto dto
    );


    // ============================================================
    // PROFILE IMAGE
    // ============================================================

    Task<MyProfileDto?> UpdateProfileImage(
        Guid userId,
        string? profileImageUrl
    );
}