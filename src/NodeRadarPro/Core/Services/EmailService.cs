using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NodeRadarPro.Data;

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
            var message = CreateMessage(settings, subject, body);
            var (ct, timeoutCts) = ResolveTimeoutToken(clientOverride != null, token);
            var client = clientOverride ?? new SmtpClient();

            try
            {
                await TransmitMessageAsync(client, settings, message, ct);
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

    private static MimeMessage CreateMessage(AppSettings settings, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("NodeRadar Pro", settings.SmtpEmail));
        message.To.Add(new MailboxAddress("Admin", settings.SmtpEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };
        return message;
    }

    private static (CancellationToken Token, CancellationTokenSource? Cts) ResolveTimeoutToken(bool hasClientOverride, CancellationToken token)
    {
        if (hasClientOverride)
            return (token, null);

        if (token == default)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return (cts.Token, cts);
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(15));
        return (linkedCts.Token, linkedCts);
    }

    private static SecureSocketOptions ResolveSecureOptions(int port) =>
        port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.StartTlsWhenAvailable
        };

    private static async Task TransmitMessageAsync(ISmtpClient client, AppSettings settings, MimeMessage message, CancellationToken ct)
    {
        var secureOptions = ResolveSecureOptions(settings.SmtpPort);
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureOptions, ct);

        if (!string.IsNullOrEmpty(settings.SmtpUser))
        {
            await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword, ct);
        }

        var request = new SmtpSendRequest(message);
        await client.SendAsync(request, ct);
        await client.DisconnectAsync(true, ct);
    }
}
