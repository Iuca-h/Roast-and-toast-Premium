using System.Net;
using System.Net.Mail;

namespace Webshop.Services;

public class EmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    // Returns true if the email was sent. Never throws, so user creation can't crash.
    public async Task<bool> SendTemporaryPasswordAsync(string email, string tempPassword)
    {
        var body = "Your WebShop account has been created.\n\n" +
                   $"Temporary password: {tempPassword}\n\n" +
                   "Please log in and change it as soon as possible.";

        // Always show the password in the console, so you can log in during development
        Console.WriteLine($"[EMAIL] To: {email}");
        Console.WriteLine($"[EMAIL] Temporary password: {tempPassword}");

        var host = _config["Smtp:Host"];
        var from = _config["Smtp:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
        {
            Console.WriteLine("[EMAIL] SMTP not configured, email not sent.");
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(from),
                Subject = "Your WebShop account",
                Body = body,
            };
            message.To.Add(email);

            using var client = new SmtpClient(host, int.Parse(_config["Smtp:Port"] ?? "587"))
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_config["Smtp:User"], _config["Smtp:Password"]),
            };

            await client.SendMailAsync(message);
            Console.WriteLine("[EMAIL] Sent.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send email to {Email}", email);
            Console.WriteLine($"[EMAIL] Send failed: {ex.Message}");
            return false;
        }
    }
}