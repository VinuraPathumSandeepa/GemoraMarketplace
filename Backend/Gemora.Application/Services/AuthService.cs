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

    public AuthService(
        ApplicationDbContext context,
        TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    // ==========================================
    // REGISTER
    // ==========================================
    public async Task<AuthResult> Register(RegisterDto dto)
    {
        var fullName = dto.FullName.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();
        var role = dto.Role.Trim();

        // Only Buyer and Seller can register publicly
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
                Message = "Only Buyer or Seller registration is allowed.",
                ErrorCode = "INVALID_ROLE"
            };
        }

        // Normalize role
        role = role.Equals(
            UserRoles.Buyer,
            StringComparison.OrdinalIgnoreCase)
                ? UserRoles.Buyer
                : UserRoles.Seller;

        // Check duplicate email
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email);

        if (emailExists)
        {
            return new AuthResult
            {
                Success = false,
                Message = "Email already registered.",
                ErrorCode = "EMAIL_EXISTS"
            };
        }

        // Hash password
        var passwordHash =
            BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Create user
        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            Message = "Registration successful."
        };
    }

    // ==========================================
    // LOGIN
    // ==========================================
    public async Task<AuthResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == email
            );

        // Same message for unknown email and bad password
        // to avoid exposing whether an account exists.
        if (user == null)
        {
            return new AuthResult
            {
                Success = false,
                Message = "Invalid email or password.",
                ErrorCode = "INVALID_CREDENTIALS"
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
                Message = "Invalid email or password.",
                ErrorCode = "INVALID_CREDENTIALS"
            };
        }

        var token = _tokenService.CreateToken(user);

        return new AuthResult
        {
            Success = true,
            Message = "Login successful.",
            Token = token
        };
    }
}