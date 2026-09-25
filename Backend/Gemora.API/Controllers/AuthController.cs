using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gemora.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ==========================================
    // REGISTER
    // POST: /api/Auth/register
    // Public endpoint
    // ==========================================
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _authService.Register(dto);

        if (!result.Success)
        {
            if (result.ErrorCode == "EMAIL_EXISTS")
            {
                return Conflict(new
                {
                    message = result.Message
                });
            }

            if (result.ErrorCode == "INVALID_ROLE")
            {
                return BadRequest(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                message = result.Message
            }
        );
    }

    // ==========================================
    // LOGIN
    // POST: /api/Auth/login
    // Public endpoint
    // ==========================================
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _authService.Login(dto);

        if (!result.Success)
        {
            if (result.ErrorCode == "INVALID_CREDENTIALS")
            {
                return Unauthorized(new
                {
                    message = result.Message
                });
            }

            return BadRequest(new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message,
            token = result.Token
        });
    }

    // ==========================================
    // CURRENT USER
    // GET: /api/Auth/me
    // JWT protected
    // ==========================================
    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        var fullName =
            User.FindFirstValue(ClaimTypes.Name);

        var email =
            User.FindFirstValue(ClaimTypes.Email);

        var role =
            User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            id = userId,
            fullName,
            email,
            role
        });
    }
}