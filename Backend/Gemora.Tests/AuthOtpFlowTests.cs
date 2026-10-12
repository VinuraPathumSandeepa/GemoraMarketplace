using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Gemora.Application.Services;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Gemora.Tests;

public class AuthOtpFlowTests
{
    private static (AuthService Service, ApplicationDbContext Database, List<string> SentCodes)
        CreateService(bool failDelivery = false)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var database = new ApplicationDbContext(options);
        var sentCodes = new List<string>();
        var email = new Mock<IEmailService>();
        var attempts = 0;
        email.Setup(service => service.SendEmailVerificationCodeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string recipientEmail, string recipientName, string code) =>
            {
                if (failDelivery && attempts++ == 0)
                {
                    return Task.FromException(
                        new InvalidOperationException("SMTP unavailable"));
                }

                sentCodes.Add(code);
                return Task.CompletedTask;
            });

        var tokenService = new TokenService(new ConfigurationBuilder().Build());
        return (new AuthService(database, tokenService, email.Object), database, sentCodes);
    }

    private static RegisterDto Registration() => new()
    {
        FullName = "OTP Test Buyer",
        Email = "otp-test@example.com",
        PhoneNumber = "+94771234567",
        CountryCode = "LK",
        Region = "Western",
        Password = "TestPassword123!",
        Role = "Buyer"
    };

    [Fact]
    public async Task RegistrationSendsSixDigitCodeAndVerificationEnablesAccount()
    {
        var (service, database, sentCodes) = CreateService();
        using (database)
        {
            var registered = await service.Register(Registration());
            Assert.True(registered.Success);
            var code = Assert.Single(sentCodes);
            Assert.Matches("^[0-9]{6}$", code);
            Assert.False((await database.Users.SingleAsync()).IsEmailVerified);

            var verified = await service.VerifyEmail(new VerifyEmailDto
            {
                Email = "otp-test@example.com",
                Code = code
            });
            Assert.True(verified.Success);
            Assert.True((await database.Users.SingleAsync()).IsEmailVerified);
        }
    }

    [Fact]
    public async Task InvalidCodeIsRejectedAndDeliveryFailureIsReported()
    {
        var (service, database, sentCodes) = CreateService();
        using (database)
        {
            await service.Register(Registration());
            var invalid = await service.VerifyEmail(new VerifyEmailDto
            {
                Email = "otp-test@example.com",
                Code = sentCodes.Single() == "000000" ? "999999" : "000000"
            });
            Assert.False(invalid.Success);
            Assert.Equal("INVALID_OTP", invalid.ErrorCode);
            Assert.False((await database.Users.SingleAsync()).IsEmailVerified);
        }

        var (failedService, failedDatabase, _) = CreateService(failDelivery: true);
        using (failedDatabase)
        {
            var registered = await failedService.Register(Registration());
            Assert.True(registered.Success);
            Assert.Equal("EMAIL_DELIVERY_FAILED", registered.ErrorCode);
            Assert.False((await failedDatabase.Users.SingleAsync()).IsEmailVerified);
            Assert.DoesNotContain(
                await failedDatabase.EmailVerificationCodes.ToListAsync(),
                code => code.UsedAt == null);

            var retry = await failedService.ResendVerificationCode(
                new ResendVerificationCodeDto { Email = "otp-test@example.com" });
            Assert.True(retry.Success);
        }
    }

    [Fact]
    public async Task ResendHonorsCooldownAndReplacesOldCode()
    {
        var (service, database, sentCodes) = CreateService();
        using (database)
        {
            await service.Register(Registration());
            var firstCode = sentCodes.Single();

            var tooSoon = await service.ResendVerificationCode(
                new ResendVerificationCodeDto { Email = "otp-test@example.com" });
            Assert.False(tooSoon.Success);
            Assert.Equal("OTP_RESEND_COOLDOWN", tooSoon.ErrorCode);

            var firstRecord = await database.EmailVerificationCodes.SingleAsync();
            firstRecord.CreatedAt = DateTime.UtcNow.AddSeconds(-61);
            await database.SaveChangesAsync();

            var resent = await service.ResendVerificationCode(
                new ResendVerificationCodeDto { Email = "otp-test@example.com" });
            Assert.True(resent.Success);
            Assert.Equal(2, sentCodes.Count);
            Assert.NotEqual(firstCode, sentCodes[1]);

            var oldCode = await service.VerifyEmail(new VerifyEmailDto
            {
                Email = "otp-test@example.com",
                Code = firstCode
            });
            Assert.False(oldCode.Success);

            var newCode = await service.VerifyEmail(new VerifyEmailDto
            {
                Email = "otp-test@example.com",
                Code = sentCodes[1]
            });
            Assert.True(newCode.Success);
        }
    }
}
