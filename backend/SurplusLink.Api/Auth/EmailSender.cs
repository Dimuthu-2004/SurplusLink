using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SurplusLink.Api.Auth;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "SurplusLink";
    public bool EnableSsl { get; set; } = true;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && Port is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(FromEmail);
}

public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message, Exception? innerException = null) : base(message, innerException) { }
}

public interface IAuthEmailSender
{
    Task SendVerificationAsync(string email, string code, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(string email, string code, CancellationToken cancellationToken);
}

public sealed class SmtpAuthEmailSender(IOptions<EmailOptions> options, ILogger<SmtpAuthEmailSender> logger) : IAuthEmailSender
{
    public Task SendVerificationAsync(string email, string code, CancellationToken cancellationToken) =>
        SendAsync(email, "Verify your SurplusLink email", "Verify your SurplusLink email", code,
            "This code expires in 10 minutes. Do not share this code. If you did not create this account, you can ignore this email.", cancellationToken);

    public Task SendPasswordResetAsync(string email, string code, CancellationToken cancellationToken) =>
        SendAsync(email, "Reset your SurplusLink password", "Reset your SurplusLink password", code,
            "This code expires in 10 minutes. Do not share this code with anyone.", cancellationToken);

    private async Task SendAsync(string to, string subject, string heading, string code, string note, CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.IsConfigured)
        {
            logger.LogError("SMTP email delivery was requested but SMTP is not configured.");
            throw new EmailDeliveryException("SMTP is not configured.");
        }

        try
        {
            using var client = new SmtpClient(config.Host, config.Port)
            {
                EnableSsl = config.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(config.Username, config.Password)
            };
            using var message = new MailMessage
            {
                From = new MailAddress(config.FromEmail, config.FromName),
                Subject = subject,
                Body = $"<main style=\"font-family:Arial,sans-serif;color:#263238;max-width:560px\"><h1 style=\"color:#d96a20\">SurplusLink</h1><h2>{heading}</h2><p>Your six-digit code is:</p><p style=\"font-size:32px;font-weight:bold;letter-spacing:8px\">{code}</p><p>{note}</p></main>",
                IsBodyHtml = true
            };
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString($"SurplusLink\n\n{heading}\n\nYour six-digit code: {code}\n\n{note}", null, "text/plain"));
            message.To.Add(to);
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (EmailDeliveryException) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "SMTP email delivery failed.");
            throw new EmailDeliveryException("SMTP delivery failed.", exception);
        }
    }
}
