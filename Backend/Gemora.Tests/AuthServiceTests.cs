using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Gemora.Tests;

public sealed class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AuthService_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Login_AcceptsExportOfficerEmailAlias()
    {
        const string password = "ExportOfficer-Test-Password";

        _context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            FullName = "Gemora Export Officer",
            Email = "export@gemora.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRoles.ExportOfficer,
            IsEmailVerified = true,
            EmailVerifiedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-only-jwt-signing-key-with-at-least-32-characters",
                ["Jwt:Issuer"] = "Gemora.Tests",
                ["Jwt:Audience"] = "Gemora.Tests"
            })
            .Build();
        var service = new AuthService(
            _context,
            new TokenService(configuration),
            Mock.Of<IEmailService>());

        var result = await service.Login(new LoginDto
        {
            Email = "  ExportOfficer@Gemora.com  ",
            Password = password
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Token);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
