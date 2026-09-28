using System.Security.Claims;

using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    // ============================================================
    // REGISTER
    //
    // POST: /api/Auth/register
    //
    // Public:
    // - Buyer
    // - Seller
    //
    // New accounts are created with:
    // IsEmailVerified = false
    //
    // An OTP is sent to the user's email.
    // ============================================================

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterDto dto)
    {
        var result =
            await _authService.Register(dto);

        if (!result.Success)
        {
            if (result.ErrorCode ==
                "EMAIL_EXISTS")
            {
                return Conflict(
                    new
                    {
                        message =
                            result.Message
                    });
            }

            if (result.ErrorCode ==
                "INVALID_ROLE")
            {
                return BadRequest(
                    new
                    {
                        message =
                            result.Message
                    });
            }

            return BadRequest(
                new
                {
                    message =
                        result.Message
                });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message =
                    result.Message,

                requiresEmailVerification =
                    true
            });
    }

    // ============================================================
    // VERIFY EMAIL
    //
    // POST: /api/Auth/verify-email
    //
    // Example:
    //
    // {
    //   "email": "buyer@example.com",
    //   "code": "482731"
    // }
    // ============================================================

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        VerifyEmailDto dto)
    {
        var result =
            await _authService.VerifyEmail(dto);

        if (!result.Success)
        {
            if (result.ErrorCode ==
                    "INVALID_OTP" ||
                result.ErrorCode ==
                    "OTP_EXPIRED" ||
                result.ErrorCode ==
                    "OTP_NOT_FOUND" ||
                result.ErrorCode ==
                    "OTP_ATTEMPTS_EXCEEDED")
            {
                return BadRequest(
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode
                    });
            }

            return BadRequest(
                new
                {
                    message =
                        result.Message
                });
        }

        return Ok(
            new
            {
                message =
                    result.Message,

                emailVerified =
                    true
            });
    }

    // ============================================================
    // RESEND EMAIL VERIFICATION CODE
    //
    // POST: /api/Auth/resend-verification-code
    //
    // Example:
    //
    // {
    //   "email": "buyer@example.com"
    // }
    // ============================================================

    [AllowAnonymous]
    [HttpPost("resend-verification-code")]
    public async Task<IActionResult>
        ResendVerificationCode(
            ResendVerificationCodeDto dto)
    {
        var result =
            await _authService
                .ResendVerificationCode(dto);

        if (!result.Success)
        {
            if (result.ErrorCode ==
                "OTP_RESEND_COOLDOWN")
            {
                return StatusCode(
                    StatusCodes
                        .Status429TooManyRequests,
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode
                    });
            }

            if (result.ErrorCode ==
                "EMAIL_DELIVERY_FAILED")
            {
                return StatusCode(
                    StatusCodes
                        .Status503ServiceUnavailable,
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode
                    });
            }

            return BadRequest(
                new
                {
                    message =
                        result.Message,

                    errorCode =
                        result.ErrorCode
                });
        }

        return Ok(
            new
            {
                message =
                    result.Message
            });
    }

    // ============================================================
    // LOGIN
    //
    // POST: /api/Auth/login
    //
    // Email must be verified before JWT is issued.
    // ============================================================

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginDto dto)
    {
        var result =
            await _authService.Login(dto);

        if (!result.Success)
        {
            if (result.ErrorCode ==
                "INVALID_CREDENTIALS")
            {
                return Unauthorized(
                    new
                    {
                        message =
                            result.Message
                    });
            }

            if (result.ErrorCode ==
                "EMAIL_NOT_VERIFIED")
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode,

                        requiresEmailVerification =
                            true
                    });
            }

            return BadRequest(
                new
                {
                    message =
                        result.Message
                });
        }

        return Ok(
            new
            {
                message =
                    result.Message,

                token =
                    result.Token
            });
    }

    // ============================================================
    // CURRENT USER
    //
    // GET: /api/Auth/me
    //
    // JWT protected
    // ============================================================

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        var fullName =
            User.FindFirstValue(
                ClaimTypes.Name);

        var email =
            User.FindFirstValue(
                ClaimTypes.Email);

        var role =
            User.FindFirstValue(
                ClaimTypes.Role);

        return Ok(
            new
            {
                userId,
                fullName,
                email,
                role
            });
    }
}