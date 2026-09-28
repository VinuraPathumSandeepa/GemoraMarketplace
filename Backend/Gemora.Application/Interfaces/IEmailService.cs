namespace Gemora.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode
    );
}