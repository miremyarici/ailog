using System.Net;
using System.Net.Mail;

namespace AIBlog.Web.Services;

public class EmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;

    public EmailService(IConfiguration configuration)
    {
        var smtp = configuration.GetSection("SmtpSettings");
        _smtpHost = smtp["Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(smtp["Port"] ?? "587");
        _smtpUsername = smtp["Username"] ?? "";
        _smtpPassword = smtp["Password"] ?? "";
        _fromEmail = smtp["From"] ?? _smtpUsername;
    }

    public async Task SendVerificationCodeAsync(string toEmail, string code)
    {
        var subject = "ailog - Email Verification Code";
        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 500px; margin: 0 auto; padding: 30px;'>
                <h1 style='color: #3d3350; font-family: Georgia, serif; text-align: center;'>ailog</h1>
                <p style='color: #4a3f5c; font-size: 16px;'>Your verification code is:</p>
                <div style='background: #f5f0f8; padding: 20px; border-radius: 12px; text-align: center; margin: 20px 0;'>
                    <span style='font-size: 32px; font-weight: bold; color: #3d3350; letter-spacing: 8px;'>{code}</span>
                </div>
                <p style='color: #8a7a9a; font-size: 14px;'>This code will expire in 10 minutes.</p>
            </div>";

        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            EnableSsl = true
        };

        var message = new MailMessage(_fromEmail, toEmail, subject, body)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(message);
    }
}
