using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using NodeRadarPro.Data;
using MailKit.Security;
using System.Net.Sockets;
using System.IO;

namespace NodeRadarPro.Core.Services;

/// <summary>
/// Static service for sending email alerts via SMTP using MailKit.
/// </summary>
public static class EmailService
{
    private const string LogSource = "EmailService";

    public static async Task SendAlertAsync(AppSettings settings, string subject, string body, LocalDatabase? db = null, ISmtpClient? clientOverride = null, CancellationToken token = default)
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

            CancellationTokenSource? timeoutCts = null;
            var ct = token;
            if (clientOverride == null && token == default)
            {
                timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                ct = timeoutCts.Token;
            }
            else if (clientOverride == null && token != default)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
                ct = timeoutCts.Token;
            }

            var client = clientOverride ?? new SmtpClient();
            try
            {
                var secureOptions = settings.SmtpPort == 465
                    ? SecureSocketOptions.SslOnConnect
                    : (settings.SmtpPort == 587
                        ? SecureSocketOptions.StartTls
                        : SecureSocketOptions.StartTlsWhenAvailable);
                await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureOptions, ct);

                if (!string.IsNullOrEmpty(settings.SmtpUser))
                {
                    await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword, ct);
                }

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
            }
            finally
            {
                timeoutCts?.Dispose();
                if (clientOverride == null)
                {
                    client.Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            targetDb.Log(LogLevel.Warning, LogSource, "SMTP operation timed out or was cancelled.");
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
