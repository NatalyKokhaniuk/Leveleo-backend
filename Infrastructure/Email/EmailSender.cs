// Services/EmailSender.cs
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace LeveLEO.Infrastructure.Email;

public class EmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpSection = configuration.GetSection("Email:Smtp");

        var host = smtpSection["Host"] ?? "smtp.gmail.com";

        var port = int.Parse(smtpSection["Port"] ?? "465");
        var username = smtpSection["Username"]!;
        var password = smtpSection["Password"]!;
        var from = smtpSection["From"] ?? "leveleo.service@gmail.com";

        var smtpClient = new SmtpClient(host)
        {
            Port = port,
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
            UseDefaultCredentials = false,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true,
        };
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        mailMessage.To.Add(email);

        await smtpClient.SendMailAsync(mailMessage);
    }
}