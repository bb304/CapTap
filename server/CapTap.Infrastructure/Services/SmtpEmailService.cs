using System.Net;
using System.Net.Mail;
using CapTap.Application.Interfaces;
using CapTap.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CapTap.Infrastructure.Services;

/// <summary>
/// SMTP email delivery. Credentials come from EmailSettings / env vars.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_settings.AppBaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
        var body =
            $"Welcome to CapTap.\n\nVerify your email by opening this link:\n{link}\n\nIf you did not create an account, ignore this message.";

        return SendAsync(email, "Verify your CapTap email", body, cancellationToken);
    }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_settings.AppBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        var body =
            $"We received a password reset request for your CapTap account.\n\nReset your password:\n{link}\n\nIf you did not request this, ignore this message.";

        return SendAsync(email, "Reset your CapTap password", body, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string textBody, CancellationToken cancellationToken)
    {
        ValidateSettings();
        cancellationToken.ThrowIfCancellationRequested();

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_settings.Username))
        {
            client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email sent to {Email} with subject {Subject}", toEmail, subject);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException(
                "SMTP email is not configured. Set EmailSettings (Host, FromEmail) or EMAIL_HOST / EMAIL_FROM.");
        }
    }
}
