using System.Net;
using Gemora.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Gemora.API.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IConfiguration configuration,
        ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailVerificationCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode)
    {
        var host =
            _configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException(
                "Email SMTP host is not configured."
            );

        var portText =
            _configuration["Email:SmtpPort"]
            ?? "587";

        if (!int.TryParse(portText, out var port))
        {
            throw new InvalidOperationException(
                "Email SMTP port is invalid."
            );
        }

        var username =
            _configuration["Email:Username"]
            ?? string.Empty;

        var password =
            _configuration["Email:Password"]
            ?? string.Empty;

        var fromEmail =
            _configuration["Email:FromEmail"]
            ?? username;

        var fromName =
            _configuration["Email:FromName"]
            ?? "Gemora";

        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "Email sender address is not configured."
            );
        }

        var safeName =
            WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(recipientName)
                    ? "Gemora User"
                    : recipientName
            );

        var safeCode =
            WebUtility.HtmlEncode(verificationCode);

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                fromName,
                fromEmail
            )
        );

        message.To.Add(
            MailboxAddress.Parse(recipientEmail)
        );

        message.Subject =
            "Verify your Gemora email";

        var bodyBuilder = new BodyBuilder
        {
            TextBody =
$"""
Hello {recipientName},

Your Gemora verification code is:

{verificationCode}

This code expires in 10 minutes.

If you did not create a Gemora account, you can ignore this email.

Gemora
""",

            HtmlBody =
$"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
</head>

<body style="
    margin:0;
    padding:0;
    background:#f5f2eb;
    font-family:Arial, Helvetica, sans-serif;
    color:#102720;
">

<table
    width="100%"
    cellpadding="0"
    cellspacing="0"
    style="
        background:#f5f2eb;
        padding:40px 20px;
    "
>
    <tr>
        <td align="center">

            <table
                width="100%"
                cellpadding="0"
                cellspacing="0"
                style="
                    max-width:600px;
                    background:#ffffff;
                    border-radius:20px;
                    overflow:hidden;
                    border:1px solid #e4ded2;
                "
            >

                <tr>
                    <td
                        style="
                            background:#08251e;
                            padding:32px;
                            text-align:center;
                        "
                    >
                        <div
                            style="
                                color:#d7a754;
                                font-size:28px;
                                font-weight:700;
                                letter-spacing:4px;
                            "
                        >
                            GEMORA
                        </div>

                        <div
                            style="
                                color:#d7ddd9;
                                font-size:12px;
                                letter-spacing:2px;
                                margin-top:8px;
                            "
                        >
                            GEMSTONE MARKETPLACE
                        </div>
                    </td>
                </tr>

                <tr>
                    <td
                        style="
                            padding:42px 40px;
                        "
                    >

                        <p
                            style="
                                font-size:16px;
                                margin-top:0;
                            "
                        >
                            Hello {safeName},
                        </p>

                        <h1
                            style="
                                font-size:26px;
                                margin:20px 0 12px;
                                color:#102720;
                            "
                        >
                            Verify your email
                        </h1>

                        <p
                            style="
                                color:#5e6d68;
                                line-height:1.6;
                            "
                        >
                            Enter the verification code below
                            to activate your Gemora account.
                        </p>

                        <div
                            style="
                                margin:32px 0;
                                padding:24px;
                                background:#faf7f0;
                                border:1px solid #eadfca;
                                border-radius:14px;
                                text-align:center;
                            "
                        >

                            <div
                                style="
                                    font-size:11px;
                                    color:#9c804e;
                                    letter-spacing:2px;
                                    font-weight:700;
                                    margin-bottom:12px;
                                "
                            >
                                VERIFICATION CODE
                            </div>

                            <div
                                style="
                                    font-size:38px;
                                    font-weight:700;
                                    letter-spacing:10px;
                                    color:#0c3329;
                                "
                            >
                                {safeCode}
                            </div>

                        </div>

                        <p
                            style="
                                color:#5e6d68;
                                line-height:1.6;
                            "
                        >
                            This code expires in
                            <strong>10 minutes</strong>.
                        </p>

                        <p
                            style="
                                color:#7a8581;
                                line-height:1.6;
                                font-size:13px;
                            "
                        >
                            If you did not create a Gemora
                            account, you can safely ignore
                            this email.
                        </p>

                    </td>
                </tr>

                <tr>
                    <td
                        style="
                            border-top:1px solid #ece7dd;
                            padding:22px;
                            text-align:center;
                            font-size:12px;
                            color:#87918d;
                        "
                    >
                        Gemora · Secure Gemstone Marketplace
                    </td>
                </tr>

            </table>

        </td>
    </tr>
</table>

</body>
</html>
"""
        };

        message.Body =
            bodyBuilder.ToMessageBody();

        using var client =
            new SmtpClient();

        try
        {
            var socketOption =
                port == 465
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls;

            await client.ConnectAsync(
                host,
                port,
                socketOption
            );

            if (!string.IsNullOrWhiteSpace(username))
            {
                await client.AuthenticateAsync(
                    username,
                    password
                );
            }

            await client.SendAsync(message);

            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Email verification code sent successfully to {Email}.",
                recipientEmail
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send verification email to {Email}.",
                recipientEmail
            );

            throw;
        }
    }
}