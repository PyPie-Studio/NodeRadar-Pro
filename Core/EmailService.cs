using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using NodeRadarPro.Data;

namespace NodeRadarPro.Core;

/// <summary>
/// Static service for sending email alerts via SMTP using MailKit.
/// </summary>
public static class EmailService
{
    public static async Task SendAlertAsync(AppSettings settings, string subject, string body)
    {
        if (!settings.EnableEmailAlerts || string.IsNullOrEmpty(settings.SmtpHost))
            return;

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("NodeRadar Pro", settings.SmtpEmail));
            message.To.Add(new MailboxAddress("Admin", settings.SmtpEmail));
            message.Subject = subject;

            message.Body = new TextPart("plain")
            {
                Text = body
            };

            using var client = new SmtpClient();
            
            // Accept all SSL certificates (optional, but often needed for local/self-signed certs)
            // client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, MailKit.Security.SecureSocketOptions.Auto);

            if (!string.IsNullOrEmpty(settings.SmtpUser))
            {
                await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            LocalDatabase.Instance.Log(LogLevel.Error, "EmailService", $"Failed to send email alert: {ex.Message}");
        }
    }
}
