using System.Security.Cryptography;

using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;

using Gemora.Domain.Constants;
using Gemora.Domain.Entities;

using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;

    private const int OtpExpiryMinutes = 10;
    private const int OtpResendCooldownSeconds = 60;
    private const int MaxOtpAttempts = 5;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public AuthService(
        ApplicationDbContext context,
        TokenService tokenService,
        IEmailService emailService)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
    }


    // ============================================================
    // REGISTER
    // ============================================================

    public async Task<AuthResult> Register(
        RegisterDto dto)
    {
        var fullName =
            dto.FullName.Trim();

        var email =
            dto.Email
                .Trim()
                .ToLowerInvariant();

        var role =
            dto.Role.Trim();

        var phoneNumber =
            dto.PhoneNumber.Trim();

        var countryCode =
            dto.CountryCode
                .Trim()
                .ToUpperInvariant();

        var region =
            dto.Region.Trim();


        // --------------------------------------------------------
        // Only Buyer and Seller can register publicly
        // --------------------------------------------------------

        if (!role.Equals(
                UserRoles.Buyer,
                StringComparison.OrdinalIgnoreCase) &&
            !role.Equals(
                UserRoles.Seller,
                StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "Only Buyer or Seller registration is allowed.",

                ErrorCode =
                    "INVALID_ROLE"
            };
        }


        // --------------------------------------------------------
        // Normalize role
        // --------------------------------------------------------

        role =
            role.Equals(
                UserRoles.Buyer,
                StringComparison.OrdinalIgnoreCase)
                ? UserRoles.Buyer
                : UserRoles.Seller;


        // --------------------------------------------------------
        // Duplicate email check
        // --------------------------------------------------------

        var emailExists =
            await _context.Users
                .AnyAsync(
                    u =>
                        u.Email.ToLower() ==
                        email
                );


        if (emailExists)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "Email already registered.",

                ErrorCode =
                    "EMAIL_EXISTS"
            };
        }


        // --------------------------------------------------------
        // Password hashing
        // --------------------------------------------------------

        var passwordHash =
            BCrypt.Net.BCrypt.HashPassword(
                dto.Password
            );


        // --------------------------------------------------------
        // Create user
        // --------------------------------------------------------

        var user =
            new User
            {
                Id =
                    Guid.NewGuid(),

                FullName =
                    fullName,

                Email =
                    email,

                PasswordHash =
                    passwordHash,

                Role =
                    role,

                PhoneNumber =
                    phoneNumber,

                CountryCode =
                    countryCode,

                Region =
                    region,

                ProfileImageUrl =
                    null,

                IsEmailVerified =
                    false,

                EmailVerifiedAt =
                    null,

                CreatedAt =
                    DateTime.UtcNow
            };


        _context.Users.Add(user);


        // --------------------------------------------------------
        // Generate secure OTP
        // --------------------------------------------------------

        var verificationCode =
            GenerateOtp();


        var codeHash =
            BCrypt.Net.BCrypt.HashPassword(
                verificationCode
            );


        var verification =
            new EmailVerificationCode
            {
                Id =
                    Guid.NewGuid(),

                UserId =
                    user.Id,

                CodeHash =
                    codeHash,

                ExpiresAt =
                    DateTime.UtcNow
                        .AddMinutes(
                            OtpExpiryMinutes
                        ),

                AttemptCount =
                    0,

                UsedAt =
                    null,

                CreatedAt =
                    DateTime.UtcNow
            };


        _context.EmailVerificationCodes.Add(
            verification
        );


        await _context.SaveChangesAsync();


        // --------------------------------------------------------
        // Send verification email
        // --------------------------------------------------------

        try
        {
            await _emailService
                .SendEmailVerificationCodeAsync(
                    user.Email,
                    user.FullName,
                    verificationCode
                );
        }
        catch
        {
            return new AuthResult
            {
                Success = true,

                Message =
                    "Your account was created, but we could not send the verification email. Please request a new verification code.",

                ErrorCode =
                    "EMAIL_DELIVERY_FAILED"
            };
        }


        return new AuthResult
        {
            Success = true,

            Message =
                "Registration successful. A 6-digit verification code has been sent to your email."
        };
    }


    // ============================================================
    // VERIFY EMAIL
    // ============================================================

    public async Task<AuthResult> VerifyEmail(
        VerifyEmailDto dto)
    {
        var email =
            dto.Email
                .Trim()
                .ToLowerInvariant();

        var code =
            dto.Code.Trim();


        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.Email.ToLower() ==
                        email
                );


        if (user == null)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "The verification code is invalid or has expired.",

                ErrorCode =
                    "INVALID_OTP"
            };
        }


        // --------------------------------------------------------
        // Already verified
        // --------------------------------------------------------

        if (user.IsEmailVerified)
        {
            return new AuthResult
            {
                Success = true,

                Message =
                    "Your email address is already verified."
            };
        }


        var verification =
            await _context
                .EmailVerificationCodes
                .Where(
                    v =>
                        v.UserId ==
                            user.Id &&
                        v.UsedAt ==
                            null
                )
                .OrderByDescending(
                    v =>
                        v.CreatedAt
                )
                .FirstOrDefaultAsync();


        if (verification == null)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "No active verification code was found. Please request a new code.",

                ErrorCode =
                    "OTP_NOT_FOUND"
            };
        }


        // --------------------------------------------------------
        // Expiry
        // --------------------------------------------------------

        if (verification.ExpiresAt <
            DateTime.UtcNow)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "Your verification code has expired. Please request a new code.",

                ErrorCode =
                    "OTP_EXPIRED"
            };
        }


        // --------------------------------------------------------
        // Attempt limit
        // --------------------------------------------------------

        if (verification.AttemptCount >=
            MaxOtpAttempts)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "Too many incorrect verification attempts. Please request a new code.",

                ErrorCode =
                    "OTP_ATTEMPTS_EXCEEDED"
            };
        }


        // --------------------------------------------------------
        // Verify OTP
        // --------------------------------------------------------

        var codeValid =
            BCrypt.Net.BCrypt.Verify(
                code,
                verification.CodeHash
            );


        if (!codeValid)
        {
            verification.AttemptCount++;


            await _context
                .SaveChangesAsync();


            var attemptsRemaining =
                MaxOtpAttempts -
                verification.AttemptCount;


            return new AuthResult
            {
                Success = false,

                Message =
                    attemptsRemaining > 0
                        ? $"Incorrect verification code. {attemptsRemaining} attempt(s) remaining."
                        : "Too many incorrect verification attempts. Please request a new code.",

                ErrorCode =
                    attemptsRemaining > 0
                        ? "INVALID_OTP"
                        : "OTP_ATTEMPTS_EXCEEDED"
            };
        }


        // --------------------------------------------------------
        // Success
        // --------------------------------------------------------

        user.IsEmailVerified =
            true;

        user.EmailVerifiedAt =
            DateTime.UtcNow;

        verification.UsedAt =
            DateTime.UtcNow;


        await _context
            .SaveChangesAsync();


        return new AuthResult
        {
            Success = true,

            Message =
                "Email verified successfully. You can now sign in to Gemora."
        };
    }


    // ============================================================
    // RESEND VERIFICATION CODE
    // ============================================================

    public async Task<AuthResult>
        ResendVerificationCode(
            ResendVerificationCodeDto dto)
    {
        var email =
            dto.Email
                .Trim()
                .ToLowerInvariant();


        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.Email.ToLower() ==
                        email
                );


        // Generic response avoids account enumeration.

        if (user == null)
        {
            return new AuthResult
            {
                Success = true,

                Message =
                    "If an unverified account exists for this email, a new verification code will be sent."
            };
        }


        if (user.IsEmailVerified)
        {
            return new AuthResult
            {
                Success = true,

                Message =
                    "This email address is already verified."
            };
        }


        // --------------------------------------------------------
        // Resend cooldown
        // --------------------------------------------------------

        var latestCode =
            await _context
                .EmailVerificationCodes
                .Where(
                    v =>
                        v.UserId ==
                        user.Id
                )
                .OrderByDescending(
                    v =>
                        v.CreatedAt
                )
                .FirstOrDefaultAsync();


        if (latestCode != null)
        {
            var secondsSinceLastCode =
                (
                    DateTime.UtcNow -
                    latestCode.CreatedAt
                )
                .TotalSeconds;


            if (secondsSinceLastCode <
                OtpResendCooldownSeconds)
            {
                var waitSeconds =
                    (int)Math.Ceiling(
                        OtpResendCooldownSeconds -
                        secondsSinceLastCode
                    );


                return new AuthResult
                {
                    Success = false,

                    Message =
                        $"Please wait {waitSeconds} second(s) before requesting another verification code.",

                    ErrorCode =
                        "OTP_RESEND_COOLDOWN"
                };
            }
        }


        // --------------------------------------------------------
        // Invalidate previous unused OTPs
        // --------------------------------------------------------

        var unusedCodes =
            await _context
                .EmailVerificationCodes
                .Where(
                    v =>
                        v.UserId ==
                            user.Id &&
                        v.UsedAt ==
                            null
                )
                .ToListAsync();


        foreach (var oldCode in unusedCodes)
        {
            oldCode.UsedAt =
                DateTime.UtcNow;
        }


        // --------------------------------------------------------
        // Create new OTP
        // --------------------------------------------------------

        var verificationCode =
            GenerateOtp();


        var codeHash =
            BCrypt.Net.BCrypt.HashPassword(
                verificationCode
            );


        var newVerification =
            new EmailVerificationCode
            {
                Id =
                    Guid.NewGuid(),

                UserId =
                    user.Id,

                CodeHash =
                    codeHash,

                ExpiresAt =
                    DateTime.UtcNow
                        .AddMinutes(
                            OtpExpiryMinutes
                        ),

                AttemptCount =
                    0,

                UsedAt =
                    null,

                CreatedAt =
                    DateTime.UtcNow
            };


        _context.EmailVerificationCodes.Add(
            newVerification
        );


        await _context
            .SaveChangesAsync();


        // --------------------------------------------------------
        // Send email
        // --------------------------------------------------------

        try
        {
            await _emailService
                .SendEmailVerificationCodeAsync(
                    user.Email,
                    user.FullName,
                    verificationCode
                );
        }
        catch
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "We could not send the verification email. Please try again.",

                ErrorCode =
                    "EMAIL_DELIVERY_FAILED"
            };
        }


        return new AuthResult
        {
            Success = true,

            Message =
                "A new 6-digit verification code has been sent to your email."
        };
    }


    // ============================================================
    // LOGIN
    // ============================================================

    public async Task<AuthResult> Login(
        LoginDto dto)
    {
        var email =
            dto.Email
                .Trim()
                .ToLowerInvariant();


        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.Email.ToLower() ==
                        email
                );


        /*
         * Use the same message for an unknown account
         * and an incorrect password.
         */

        if (user == null)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "Invalid email or password.",

                ErrorCode =
                    "INVALID_CREDENTIALS"
            };
        }


        var passwordValid =
            BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.PasswordHash
            );


        if (!passwordValid)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "Invalid email or password.",

                ErrorCode =
                    "INVALID_CREDENTIALS"
            };
        }


        // --------------------------------------------------------
        // Email must be verified
        // --------------------------------------------------------

        if (!user.IsEmailVerified)
        {
            return new AuthResult
            {
                Success = false,

                Message =
                    "Please verify your email before signing in.",

                ErrorCode =
                    "EMAIL_NOT_VERIFIED"
            };
        }


        // --------------------------------------------------------
        // Generate JWT
        // --------------------------------------------------------

        var token =
            _tokenService.CreateToken(
                user
            );


        return new AuthResult
        {
            Success = true,

            Message =
                "Login successful.",

            Token =
                token
        };
    }


    // ============================================================
    // GET CURRENT USER PROFILE
    // ============================================================

    public async Task<MyProfileDto?> GetMyProfile(
        Guid userId)
    {
        var user =
            await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u =>
                        u.Id ==
                        userId
                );


        if (user == null)
        {
            return null;
        }


        return MapToMyProfileDto(
            user
        );
    }


    // ============================================================
    // UPDATE CURRENT USER PROFILE
    //
    // Editable:
    // - FullName
    // - PhoneNumber
    // - CountryCode
    // - Region
    //
    // Protected:
    // - ID
    // - Email
    // - Role
    // - PasswordHash
    // - Email verification status
    // - Profile image
    // ============================================================

    public async Task<MyProfileDto?> UpdateMyProfile(
        Guid userId,
        UpdateMyProfileDto dto)
    {
        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.Id ==
                        userId
                );


        if (user == null)
        {
            return null;
        }


        var fullName =
            dto.FullName.Trim();


        var phoneNumber =
            dto.PhoneNumber?
                .Trim() ??
            string.Empty;


        var countryCode =
            dto.CountryCode?
                .Trim()
                .ToUpperInvariant() ??
            string.Empty;


        var region =
            dto.Region?
                .Trim() ??
            string.Empty;


        user.FullName =
            fullName;

        user.PhoneNumber =
            phoneNumber;

        user.CountryCode =
            countryCode;

        user.Region =
            region;


        await _context
            .SaveChangesAsync();


        return MapToMyProfileDto(
            user
        );
    }


    // ============================================================
    // UPDATE PROFILE IMAGE URL
    //
    // File storage itself is handled in the API layer.
    // This method only updates PostgreSQL.
    // ============================================================

    public async Task<MyProfileDto?> UpdateProfileImage(
        Guid userId,
        string? profileImageUrl)
    {
        var user =
            await _context.Users
                .FirstOrDefaultAsync(
                    u =>
                        u.Id ==
                        userId
                );


        if (user == null)
        {
            return null;
        }


        user.ProfileImageUrl =
            string.IsNullOrWhiteSpace(
                profileImageUrl)
                ? null
                : profileImageUrl.Trim();


        await _context
            .SaveChangesAsync();


        return MapToMyProfileDto(
            user
        );
    }


    // ============================================================
    // PROFILE MAPPER
    // ============================================================

    private static MyProfileDto MapToMyProfileDto(
        User user)
    {
        return new MyProfileDto
        {
            Id =
                user.Id,

            FullName =
                user.FullName,

            Email =
                user.Email,

            Role =
                user.Role,

            PhoneNumber =
                user.PhoneNumber,

            CountryCode =
                user.CountryCode,

            Region =
                user.Region,

            ProfileImageUrl =
                user.ProfileImageUrl,

            IsEmailVerified =
                user.IsEmailVerified,

            EmailVerifiedAt =
                user.EmailVerifiedAt,

            CreatedAt =
                user.CreatedAt
        };
    }


    // ============================================================
    // GENERATE SECURE OTP
    // ============================================================

    private static string GenerateOtp()
    {
        var number =
            RandomNumberGenerator.GetInt32(
                100000,
                1000000
            );


        return number.ToString();
    }
}