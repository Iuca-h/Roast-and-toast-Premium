using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Webshop.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IConfiguration config,
        ILogger<EmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string email,
        string subject,
        string htmlMessage)
    {
        var host = _config["Smtp:Host"];
        var from = _config["Smtp:From"];
        var user = _config["Smtp:User"];
        var password = _config["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SMTP configuration is incomplete.");
        }

        if (!int.TryParse(_config["Smtp:Port"], out var port))
        {
            throw new InvalidOperationException(
                "Invalid SMTP port.");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(from),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(new MailAddress(email));

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(user, password)
            };

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email.");

            throw;
        }
    }
}
