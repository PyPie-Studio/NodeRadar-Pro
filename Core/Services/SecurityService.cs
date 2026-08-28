using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NodeRadarPro.Core;

/// <summary>
/// Provides security-related utility functions, such as hashing and constant-time verification.
/// </summary>
public static class SecurityService
{
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
}
