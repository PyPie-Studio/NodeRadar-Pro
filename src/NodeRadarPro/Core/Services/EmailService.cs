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
    private const string LogSource = "EmailService";

    public static async Task SendAlertAsync(AppSettings settings, string subject, string body, LocalDatabase? db = null, ISmtpClient? clientOverride = null)
    {
        if (!settings.EnableEmailAlerts || string.IsNullOrEmpty(settings.SmtpHost))
            return;

        var targetDb = db ?? LocalDatabase.Instance;

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

            var client = clientOverride ?? new SmtpClient();
            try
            {
                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, MailKit.Security.SecureSocketOptions.Auto);

                if (!string.IsNullOrEmpty(settings.SmtpUser))
                {
                    await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            finally
            {
                if (clientOverride == null)
                {
                    client.Dispose();
                }
            }
        }
        catch (AuthenticationException ex)
        {
            targetDb.Log(LogLevel.Error, LogSource, $"SMTP Authentication failed: {ex.Message}");
        }
        catch (SmtpCommandException ex)
        {
            targetDb.Log(LogLevel.Error, LogSource, $"SMTP Command failed: {ex.Message}");
        }
        catch (SmtpProtocolException ex)
        {
            targetDb.Log(LogLevel.Error, LogSource, $"SMTP Protocol error: {ex.Message}");
        }
        catch (SocketException ex)
        {
            targetDb.Log(LogLevel.Error, LogSource, $"Network error: {ex.Message}");
        }
        catch (IOException ex)
        {
            targetDb.Log(LogLevel.Error, LogSource, $"I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
            targetDb.Log(LogLevel.Error, LogSource, $"Failed to send email alert: {ex.Message}");
        }
    }
}
