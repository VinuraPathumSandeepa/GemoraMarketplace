using System.Security.Claims;

using Gemora.API.Services;

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

    private readonly IProfileImageStorageService
        _profileImageStorageService;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public AuthController(
        IAuthService authService,
        IProfileImageStorageService profileImageStorageService)
    {
        _authService =
            authService;

        _profileImageStorageService =
            profileImageStorageService;
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
    // Email verification is required before login.
    // ============================================================

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterDto dto)
    {
        var result =
            await _authService
                .Register(dto);


        if (!result.Success)
        {
            if (result.ErrorCode ==
                "EMAIL_EXISTS")
            {
                return Conflict(
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode
                    }
                );
            }


            if (result.ErrorCode ==
                "INVALID_ROLE")
            {
                return BadRequest(
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode
                    }
                );
            }


            return BadRequest(
                new
                {
                    message =
                        result.Message,

                    errorCode =
                        result.ErrorCode
                }
            );
        }


        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message =
                    result.Message,

                requiresEmailVerification =
                    true
            }
        );
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
            await _authService
                .VerifyEmail(dto);


        if (!result.Success)
        {
            if (
                result.ErrorCode ==
                    "INVALID_OTP" ||
                result.ErrorCode ==
                    "OTP_EXPIRED" ||
                result.ErrorCode ==
                    "OTP_NOT_FOUND" ||
                result.ErrorCode ==
                    "OTP_ATTEMPTS_EXCEEDED"
            )
            {
                return BadRequest(
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode
                    }
                );
            }


            return BadRequest(
                new
                {
                    message =
                        result.Message,

                    errorCode =
                        result.ErrorCode
                }
            );
        }


        return Ok(
            new
            {
                message =
                    result.Message,

                emailVerified =
                    true
            }
        );
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
                    }
                );
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
                    }
                );
            }


            return BadRequest(
                new
                {
                    message =
                        result.Message,

                    errorCode =
                        result.ErrorCode
                }
            );
        }


        return Ok(
            new
            {
                message =
                    result.Message
            }
        );
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
            await _authService
                .Login(dto);


        if (!result.Success)
        {
            if (result.ErrorCode ==
                "INVALID_CREDENTIALS")
            {
                return Unauthorized(
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode
                    }
                );
            }


            if (result.ErrorCode ==
                "EMAIL_NOT_VERIFIED")
            {
                return StatusCode(
                    StatusCodes
                        .Status403Forbidden,
                    new
                    {
                        message =
                            result.Message,

                        errorCode =
                            result.ErrorCode,

                        requiresEmailVerification =
                            true
                    }
                );
            }


            return BadRequest(
                new
                {
                    message =
                        result.Message,

                    errorCode =
                        result.ErrorCode
                }
            );
        }


        return Ok(
            new
            {
                message =
                    result.Message,

                token =
                    result.Token
            }
        );
    }


    // ============================================================
    // CURRENT USER PROFILE
    //
    // GET: /api/Auth/me
    //
    // JWT identifies the user.
    // Current profile data is loaded from PostgreSQL.
    // ============================================================

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId =
            GetAuthenticatedUserId();


        if (userId == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your session is invalid. Please sign in again."
                }
            );
        }


        var profile =
            await _authService
                .GetMyProfile(
                    userId.Value
                );


        if (profile == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account could not be found. Please sign in again."
                }
            );
        }


        return Ok(profile);
    }


    // ============================================================
    // UPDATE CURRENT USER PROFILE
    //
    // PUT: /api/Auth/me/profile
    //
    // User may update:
    // - FullName
    // - PhoneNumber
    // - CountryCode
    // - Region
    //
    // User may NOT update:
    // - ID
    // - Email
    // - Role
    // - Password
    // - Verification status
    // ============================================================

    [Authorize]
    [HttpPut("me/profile")]
    public async Task<IActionResult>
        UpdateCurrentUserProfile(
            UpdateMyProfileDto dto)
    {
        var userId =
            GetAuthenticatedUserId();


        if (userId == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your session is invalid. Please sign in again."
                }
            );
        }


        var profile =
            await _authService
                .UpdateMyProfile(
                    userId.Value,
                    dto
                );


        if (profile == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account could not be found. Please sign in again."
                }
            );
        }


        return Ok(
            new
            {
                message =
                    "Your profile has been updated successfully.",

                profile
            }
        );
    }


    // ============================================================
    // UPLOAD / REPLACE PROFILE IMAGE
    //
    // POST: /api/Auth/me/profile-image
    //
    // multipart/form-data
    //
    // Field name:
    // file
    //
    // Allowed:
    // - JPG / JPEG
    // - PNG
    // - WEBP
    //
    // Maximum:
    // - 5 MB
    //
    // IMPORTANT:
    // Do NOT put [FromForm] directly on IFormFile here.
    // Swashbuckle can fail while generating swagger.json when
    // [FromForm] is used directly with IFormFile.
    // ============================================================

    [Authorize]
    [HttpPost("me/profile-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProfileImage(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId =
            GetAuthenticatedUserId();


        if (userId == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your session is invalid. Please sign in again."
                }
            );
        }


        if (file == null ||
            file.Length <= 0)
        {
            return BadRequest(
                new
                {
                    message =
                        "Please select a profile image."
                }
            );
        }


        // --------------------------------------------------------
        // Load existing profile
        // --------------------------------------------------------

        var currentProfile =
            await _authService
                .GetMyProfile(
                    userId.Value
                );


        if (currentProfile == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account could not be found. Please sign in again."
                }
            );
        }


        string? newImageUrl =
            null;


        try
        {
            // ----------------------------------------------------
            // Save new image
            // ----------------------------------------------------

            newImageUrl =
                await _profileImageStorageService
                    .SaveAsync(
                        userId.Value,
                        file,
                        cancellationToken
                    );


            // ----------------------------------------------------
            // Save new image URL to PostgreSQL
            // ----------------------------------------------------

            var updatedProfile =
                await _authService
                    .UpdateProfileImage(
                        userId.Value,
                        newImageUrl
                    );


            if (updatedProfile == null)
            {
                // User vanished before DB update.
                // Remove newly created orphan file.

                await SafeDeleteProfileImageAsync(
                    newImageUrl,
                    cancellationToken
                );


                return Unauthorized(
                    new
                    {
                        message =
                            "Your account could not be found. Please sign in again."
                    }
                );
            }


            // ----------------------------------------------------
            // Delete previous profile image
            //
            // Do this only AFTER PostgreSQL has successfully
            // stored the new image URL.
            //
            // Failure to clean an old file should not destroy
            // the newly saved profile image.
            // ----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                    currentProfile.ProfileImageUrl) &&
                !string.Equals(
                    currentProfile.ProfileImageUrl,
                    newImageUrl,
                    StringComparison.OrdinalIgnoreCase))
            {
                await SafeDeleteProfileImageAsync(
                    currentProfile.ProfileImageUrl,
                    cancellationToken
                );
            }


            return Ok(
                new
                {
                    message =
                        "Your profile photo has been updated successfully.",

                    profile =
                        updatedProfile
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            // Validation errors from image storage:
            // - unsupported image
            // - too large
            // - invalid signature

            if (!string.IsNullOrWhiteSpace(
                newImageUrl))
            {
                await SafeDeleteProfileImageAsync(
                    newImageUrl,
                    cancellationToken
                );
            }


            return BadRequest(
                new
                {
                    message =
                        ex.Message
                }
            );
        }
        catch
        {
            // ----------------------------------------------------
            // Unexpected failure.
            //
            // If a new physical file exists but the operation
            // didn't complete, attempt cleanup.
            // ----------------------------------------------------

            if (!string.IsNullOrWhiteSpace(
                newImageUrl))
            {
                await SafeDeleteProfileImageAsync(
                    newImageUrl,
                    cancellationToken
                );
            }


            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,
                new
                {
                    message =
                        "We couldn't update your profile photo. Please try again."
                }
            );
        }
    }


    // ============================================================
    // REMOVE PROFILE IMAGE
    //
    // DELETE: /api/Auth/me/profile-image
    //
    // Database value becomes NULL.
    // Frontend can then use initials as fallback.
    // ============================================================

    [Authorize]
    [HttpDelete("me/profile-image")]
    public async Task<IActionResult> RemoveProfileImage(
        CancellationToken cancellationToken)
    {
        var userId =
            GetAuthenticatedUserId();


        if (userId == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your session is invalid. Please sign in again."
                }
            );
        }


        var currentProfile =
            await _authService
                .GetMyProfile(
                    userId.Value
                );


        if (currentProfile == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account could not be found. Please sign in again."
                }
            );
        }


        var oldImageUrl =
            currentProfile.ProfileImageUrl;


        // --------------------------------------------------------
        // Clear PostgreSQL reference FIRST.
        // --------------------------------------------------------

        var updatedProfile =
            await _authService
                .UpdateProfileImage(
                    userId.Value,
                    null
                );


        if (updatedProfile == null)
        {
            return Unauthorized(
                new
                {
                    message =
                        "Your account could not be found. Please sign in again."
                }
            );
        }


        // --------------------------------------------------------
        // Physical file cleanup is secondary.
        //
        // Even if cleanup fails, the image is no longer attached
        // to the user's account in PostgreSQL.
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
            oldImageUrl))
        {
            await SafeDeleteProfileImageAsync(
                oldImageUrl,
                cancellationToken
            );
        }


        return Ok(
            new
            {
                message =
                    "Your profile photo has been removed.",

                profile =
                    updatedProfile
            }
        );
    }


    // ============================================================
    // GET AUTHENTICATED USER ID
    //
    // User ID always comes from the signed JWT.
    //
    // The client is never allowed to choose another UserId
    // when editing its own profile.
    // ============================================================

    private Guid? GetAuthenticatedUserId()
    {
        var userIdClaim =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );


        if (string.IsNullOrWhiteSpace(
            userIdClaim))
        {
            return null;
        }


        if (!Guid.TryParse(
            userIdClaim,
            out var userId))
        {
            return null;
        }


        return userId;
    }


    // ============================================================
    // SAFE PROFILE IMAGE DELETE
    //
    // File cleanup should not cause an otherwise successful
    // profile operation to fail.
    // ============================================================

    private async Task SafeDeleteProfileImageAsync(
        string? profileImageUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
            profileImageUrl))
        {
            return;
        }


        try
        {
            await _profileImageStorageService
                .DeleteAsync(
                    profileImageUrl,
                    cancellationToken
                );
        }
        catch
        {
            // Intentionally ignore cleanup failure.
            //
            // A later maintenance process can remove orphan files.
            // We should not invalidate a successfully updated
            // database profile because an old physical file
            // could not be deleted.
        }
    }
}