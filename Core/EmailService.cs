using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using NodeRadarPro.Data;
using MailKit.Security;
using System.Net.Sockets;
using System.IO;

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
        catch (AuthenticationException ex)
        {
            LocalDatabase.Instance.Log(LogLevel.Error, "EmailService", $"SMTP Authentication failed: {ex.Message}");
        }
        catch (SmtpCommandException ex)
        {
            LocalDatabase.Instance.Log(LogLevel.Error, "EmailService", $"SMTP Command failed: {ex.Message}");
        }
        catch (SmtpProtocolException ex)
        {
            LocalDatabase.Instance.Log(LogLevel.Error, "EmailService", $"SMTP Protocol error: {ex.Message}");
        }
        catch (SocketException ex)
        {
            LocalDatabase.Instance.Log(LogLevel.Error, "EmailService", $"Network error: {ex.Message}");
        }
        catch (IOException ex)
        {
            LocalDatabase.Instance.Log(LogLevel.Error, "EmailService", $"I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
            LocalDatabase.Instance.Log(LogLevel.Error, "EmailService", $"Failed to send email alert: {ex.Message}");
        }
    }
}
