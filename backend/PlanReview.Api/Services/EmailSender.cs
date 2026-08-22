using System.Net;
using System.Net.Mail;

namespace PlanReview.Api.Services;

public class EmailOptions
{
    /// <summary>When false (default), emails are logged only — never actually sent.</summary>
    public bool Enabled { get; set; }
    public string From { get; set; } = "arise@sparrow.local";
    public string FromName { get; set; } = "ARISe";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public bool UseSsl { get; set; } = true;
}

public interface IEmailSender
{
    /// <summary>Returns true if the message was actually transmitted over SMTP.</summary>
    Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// Sends over SMTP when email is enabled and configured; otherwise logs the message
/// (so the feature is fully exercisable in dev without emailing real addresses).
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<EmailSender> _log;

    public EmailSender(EmailOptions opts, ILogger<EmailSender> log)
    {
        _opts = opts;
        _log = log;
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        if (!_opts.Enabled || string.IsNullOrWhiteSpace(_opts.SmtpHost))
        {
            _log.LogInformation("[email:disabled] To={To} | Subject={Subject}\n{Body}", toEmail, subject, body);
            return false;
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(_opts.From, _opts.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
        };
        msg.To.Add(toEmail);

        using var client = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
        {
            EnableSsl = _opts.UseSsl,
            Credentials = string.IsNullOrEmpty(_opts.SmtpUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_opts.SmtpUser, _opts.SmtpPassword),
        };

        try
        {
            await client.SendMailAsync(msg, ct);
            _log.LogInformation("[email:sent] To={To} | Subject={Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[email:failed] To={To} | Subject={Subject}", toEmail, subject);
            return false;
        }
    }
}
