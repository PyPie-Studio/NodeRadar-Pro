using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NodeRadarPro.Data;

namespace NodeRadarPro.Core;

/// <summary>
/// Provides security-related utility functions, such as hashing and constant-time verification.
/// </summary>
public static class SecurityService
{
    private const string LogSecurity = "Security";

    /// <summary>
    /// Computes the SHA256 hash of a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>A lowercase hex string representing the SHA256 hash, or null if the file does not exist.</returns>
    public static string? ComputeFileHash(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Performs constant-time comparison between two hash strings to prevent timing attacks.
    /// </summary>
    public static bool VerifyHash(string? hashA, string? hashB)
    {
        if (hashA == null || hashB == null) return false;
        if (hashA.Length != hashB.Length) return false;

        byte[] aBytes = Encoding.UTF8.GetBytes(hashA.ToLowerInvariant());
        byte[] bBytes = Encoding.UTF8.GetBytes(hashB.ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    /// <summary>
    /// Verifies that a file's SHA256 matches the expected hash in constant time.
    /// </summary>
    public static bool VerifyFileHash(string filePath, string expectedHash)
    {
        string? computed = ComputeFileHash(filePath);
        return VerifyHash(computed, expectedHash);
    }

    /// <summary>
    /// Verifies system integrity by checking the binary file hash against the last known good hash.
    /// Incorporates the application version to prevent false-positive tampering warnings across version upgrades.
    /// </summary>
    public static void VerifySystemIntegrity(AppSettings settings, LocalDatabase db, string? versionOverride = null)
    {
        try
        {
            string mainFile = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NodeRadarPro.dll");

            if (!File.Exists(mainFile))
            {
                return;
            }

            string? currentHash = ComputeFileHash(mainFile);
            if (string.IsNullOrEmpty(currentHash))
            {
                return;
            }

            string version = versionOverride ?? (typeof(SecurityService).Assembly.GetName().Version is { } v
                ? $"{v.Major}.{v.Minor}.{v.Build}"
                : "1.0.0");

            string expectedVersionPrefix = $"{version}:";

            if (string.IsNullOrEmpty(settings.LastKnownGoodHash) ||
                settings.LastKnownGoodHash == "INITIAL" ||
                !settings.LastKnownGoodHash.StartsWith(expectedVersionPrefix, StringComparison.Ordinal))
            {
                settings.LastKnownGoodHash = expectedVersionPrefix + currentHash;
                db.SaveSettings(settings);
                db.Log(LogLevel.Info, LogSecurity, $"System integrity signature locked for v{version}.");
            }
            else
            {
                string storedHash = settings.LastKnownGoodHash.Substring(expectedVersionPrefix.Length);
                if (!VerifyHash(storedHash, currentHash))
                {
                    db.Log(LogLevel.Warning, LogSecurity, "CORE INTEGRITY MISMATCH: Application binary may have been tampered with!");
                }
                else
                {
                    db.Log(LogLevel.Info, LogSecurity, "System integrity verified (SHA256).");
                }
            }
        }
        catch (Exception ex)
        {
            db.Log(LogLevel.Error, LogSecurity, $"System integrity check failed: {ex.Message}");
        }
    }
}
