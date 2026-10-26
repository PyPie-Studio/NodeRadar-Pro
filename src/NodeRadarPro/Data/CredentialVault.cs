using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NodeRadarPro.Data;

/// <summary>
/// Centralized credential encryption, decryption, and DPAPI key management.
/// Handles Windows DPAPI primary path and cross-platform AES-GCM PBKDF2 fallback.
/// </summary>
public static class CredentialVault
{
    // ── Database Password Management ──

    public static string GetOrGenerateDbPassword(string folder)
    {
        string keyFile = Path.Combine(folder, "db_key.bin");
        if (File.Exists(keyFile))
        {
            try
            {
                byte[] encrypted = File.ReadAllBytes(keyFile);
                byte[] decrypted = UnprotectData(encrypted);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                // If decryption fails (e.g., moved to another machine), fallback to a new password
                // Note: The existing DB won't be openable, but returning a new password avoids a crash here.
            }
        }

        // Generate a new secure password
        byte[] secret = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secret);
        }
        string newPassword = Convert.ToBase64String(secret);
        try
        {
            SaveDbPassword(folder, newPassword);

            // Rename database since the old key is lost
            string dbPath = Path.Combine(folder, "noderadar.db");
            DatabaseBackupService.RotateCorruptDatabase(dbPath);
        }
        catch
        {
            // Fallback ignore
        }
        return newPassword;
    }

    public static void SaveDbPassword(string folder, string password)
    {
        string keyFile = Path.Combine(folder, "db_key.bin");
        byte[] secret = Encoding.UTF8.GetBytes(password);
        byte[] encrypted = ProtectData(secret);
        File.WriteAllBytes(keyFile, encrypted);
    }

    // ── Application Secret Encryption ──

    public static string EncryptSecret(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        var secret = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectData(secret);
        return Convert.ToBase64String(encrypted);
    }

    public static string DecryptSecret(string encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64)) return "";
        byte[] data;
        try
        {
            data = Convert.FromBase64String(encryptedBase64);
        }
        catch
        {
            return "";
        }

        try
        {
            byte[] decrypted = UnprotectData(data);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return "";
        }
    }

    // ── Protection Helpers (DPAPI with AES-GCM PBKDF2 Fallback) ──

    internal static byte[] ProtectData(byte[] userData)
    {
        try
        {
            return ProtectedData.Protect(userData, null, DataProtectionScope.CurrentUser);
        }
        catch (PlatformNotSupportedException)
        {
            byte[] salt = new byte[16];
            byte[] nonce = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(nonce);
            }

            byte[] key = DeriveKeyPbkdf2(salt);
            byte[] cipherText = new byte[userData.Length];
            byte[] tag = new byte[16];

            using (var aesGcm = new AesGcm(key, 16))
            {
                aesGcm.Encrypt(nonce, userData, cipherText, tag);
            }

            // Version 0x02 format: [1 byte version (0x02)][16 bytes salt][12 bytes nonce][16 bytes tag][cipherText]
            byte[] result = new byte[1 + 16 + 12 + 16 + cipherText.Length];
            result[0] = 0x02;
            Buffer.BlockCopy(salt, 0, result, 1, 16);
            Buffer.BlockCopy(nonce, 0, result, 17, 12);
            Buffer.BlockCopy(tag, 0, result, 29, 16);
            Buffer.BlockCopy(cipherText, 0, result, 45, cipherText.Length);
            return result;
        }
    }

    internal static byte[] UnprotectData(byte[] data)
    {
        try
        {
            return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException || ex is CryptographicException)
        {
            if (data.Length >= 45 && data[0] == 0x02)
            {
                try
                {
                    byte[] salt = new byte[16];
                    byte[] nonce = new byte[12];
                    byte[] tag = new byte[16];
                    byte[] cipherText = new byte[data.Length - 45];

                    Buffer.BlockCopy(data, 1, salt, 0, 16);
                    Buffer.BlockCopy(data, 17, nonce, 0, 12);
                    Buffer.BlockCopy(data, 29, tag, 0, 16);
                    Buffer.BlockCopy(data, 45, cipherText, 0, cipherText.Length);

                    byte[] key = DeriveKeyPbkdf2(salt);
                    byte[] plainBytes = new byte[cipherText.Length];
                    try
                    {
                        using (var aesGcm = new AesGcm(key, 16))
                        {
                            aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);
                        }
                        return plainBytes;
                    }
                    catch (CryptographicException)
                    {
                        // Fallback to legacy machine-name based key derivation for backward compatibility
                        byte[] legacyKey = DeriveKeyPbkdf2Legacy(salt);
                        using (var aesGcm = new AesGcm(legacyKey, 16))
                        {
                            aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);
                        }
                        return plainBytes;
                    }
                }
                catch
                {
                    return data;
                }
            }
            if (data.Length >= 29 && data[0] == 0x01)
            {
                try
                {
                    byte[] key = GetFallbackEncryptionKeyLegacy();
                    byte[] nonce = new byte[12];
                    byte[] tag = new byte[16];
                    byte[] cipherText = new byte[data.Length - 29];

                    Buffer.BlockCopy(data, 1, nonce, 0, 12);
                    Buffer.BlockCopy(data, 13, tag, 0, 16);
                    Buffer.BlockCopy(data, 29, cipherText, 0, cipherText.Length);

                    byte[] plainBytes = new byte[cipherText.Length];
                    using (var aesGcm = new AesGcm(key, 16))
                    {
                        aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);
                    }
                    return plainBytes;
                }
                catch
                {
                    return data;
                }
            }
            return data;
        }
    }

    // ── Internal Key Derivation & Secret Management ──

    private static readonly object SecretLock = new();
    private static byte[]? _cachedVaultSecret;

    internal static byte[] GetOrCreateVaultSecret()
    {
        lock (SecretLock)
        {
            if (_cachedVaultSecret != null)
            {
                return (byte[])_cachedVaultSecret.Clone();
            }

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NodeRadarPro");
            string secretFile = Path.Combine(dir, ".vault_secret");

            if (File.Exists(secretFile))
            {
                try
                {
                    byte[] secretBytes = File.ReadAllBytes(secretFile);
                    if (secretBytes.Length == 32)
                    {
                        _cachedVaultSecret = secretBytes;
                        return (byte[])secretBytes.Clone();
                    }
                }
                catch
                {
                    // Fallback to generating a new key if read fails
                }
            }

            byte[] newSecret = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newSecret);
            }

            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(secretFile, newSecret);

                if (!OperatingSystem.IsWindows())
                {
                    try
                    {
                        File.SetUnixFileMode(secretFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    }
                    catch
                    {
                        // Platform or filesystem doesn't support Unix file permissions
                    }
                }
            }
            catch
            {
                // Fallback ignore writing error, keep in-memory secret
            }

            _cachedVaultSecret = newSecret;
            return (byte[])newSecret.Clone();
        }
    }

    internal static void ResetCachedSecretForTesting()
    {
        lock (SecretLock)
        {
            _cachedVaultSecret = null;
        }
    }

    internal static byte[] DeriveKeyPbkdf2(byte[] salt)
    {
        byte[] secret = GetOrCreateVaultSecret();
        return Rfc2898DeriveBytes.Pbkdf2(secret, salt, 100000, HashAlgorithmName.SHA256, 32);
    }

    internal static byte[] DeriveKeyPbkdf2Legacy(byte[] salt)
    {
        string password = $"{Environment.MachineName}_{Environment.UserName}_NodeRadarPro_Pbkdf2Secret";
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
    }

    internal static byte[] GetFallbackEncryptionKeyLegacy()
    {
        string identifier = $"{Environment.MachineName}_{Environment.UserName}_NodeRadarPro_FallbackKey";
        return SHA256.HashData(Encoding.UTF8.GetBytes(identifier));
    }
}
