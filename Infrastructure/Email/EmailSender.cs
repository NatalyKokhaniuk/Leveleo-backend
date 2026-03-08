// Services/EmailSender.cs
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace LeveLEO.Infrastructure.Email;

public class EmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var smtpSection = configuration.GetSection("SMS:Smtp");

        var host = smtpSection["Host"] ?? "smtp.gmail.com";

        var port = int.Parse(smtpSection["Port"] ?? "587");
        var username = smtpSection["Username"]!;
        var password = smtpSection["Password"]!;
        var from = smtpSection["From"] ?? username;

        var smtpClient = new SmtpClient(host)
        {
            Port = port,
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true,
        };
        mailMessage.To.Add(email);

        await smtpClient.SendMailAsync(mailMessage);
    }
}