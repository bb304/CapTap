using CapTap.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CapTap.Infrastructure.Services;

/// <summary>
/// Placeholder email sender. Does not deliver mail; logs intent only.
/// Sensitive tokens are never logged outside Development.
/// </summary>
public sealed class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;
    private readonly IHostEnvironment _environment;

    public MockEmailService(ILogger<MockEmailService> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "Verification email would be sent to {Email}. Dev token: {Token}",
                email,
                verificationToken);
        }
        else
        {
            _logger.LogInformation("Verification email would be sent to {Email}.", email);
        }

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "Password reset email would be sent to {Email}. Dev token: {Token}",
                email,
                resetToken);
        }
        else
        {
            _logger.LogInformation("Password reset email would be sent to {Email}.", email);
        }

        return Task.CompletedTask;
    }
}
