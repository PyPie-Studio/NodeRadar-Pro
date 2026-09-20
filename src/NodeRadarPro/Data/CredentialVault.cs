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
    private static readonly object SyncLock = new object();

    // ── Database Password Management ──

    public static string GetOrGenerateDbPassword(string folder)
    {
        if (Environment.GetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING") == "true")
            return "test_password";

        string keyFile = Path.Combine(folder, "db_key.bin");
        if (File.Exists(keyFile))
        {
            try
            {
                byte[] encrypted = File.ReadAllBytes(keyFile);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                // If DPAPI decryption fails (e.g., moved to another machine), fallback to a new password
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
        if (Environment.GetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING") == "true")
            return;

        string keyFile = Path.Combine(folder, "db_key.bin");
        byte[] secret = Encoding.UTF8.GetBytes(password);
        byte[] encrypted = ProtectedData.Protect(secret, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyFile, encrypted);
    }

    // ── Application Secret Encryption ──

    public static string EncryptSecret(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        try
        {
            var secret = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(secret, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException || ex is CryptographicException)
        {
            byte[] salt = new byte[16];
            byte[] nonce = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(nonce);
            }

            byte[] key = DeriveKeyPbkdf2(salt);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherText = new byte[plainBytes.Length];
            byte[] tag = new byte[16];

            using (var aesGcm = new AesGcm(key, 16))
            {
                aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
            }

            // Version 0x02 format: [1 byte version (0x02)][16 bytes salt][12 bytes nonce][16 bytes tag][cipherText]
            byte[] result = new byte[1 + 16 + 12 + 16 + cipherText.Length];
            result[0] = 0x02;
            Buffer.BlockCopy(salt, 0, result, 1, 16);
            Buffer.BlockCopy(nonce, 0, result, 17, 12);
            Buffer.BlockCopy(tag, 0, result, 29, 16);
            Buffer.BlockCopy(cipherText, 0, result, 45, cipherText.Length);
            return Convert.ToBase64String(result);
        }
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
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException || ex is CryptographicException)
        {
            if (data.Length >= 45 && data[0] == 0x02)
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
                    return Encoding.UTF8.GetString(plainBytes);
                }
                catch (CryptographicException)
                {
                    byte[] legacyKey = DeriveKeyPbkdf2Legacy(salt);
                    using (var aesGcm = new AesGcm(legacyKey, 16))
                    {
                        aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);
                    }
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
            if (data.Length >= 29 && data[0] == 0x01)
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
                return Encoding.UTF8.GetString(plainBytes);
            }
            return Encoding.UTF8.GetString(data);
        }
    }

    // ── Internal Key Derivation ──

    internal static byte[] GetOrGenerateFallbackSecret()
    {
        lock (SyncLock)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PyPie Studio", "NodeRadar Pro");
            Directory.CreateDirectory(folder);
            string secretPath = Path.Combine(folder, "vault_secret.bin");

            if (File.Exists(secretPath))
            {
                try
                {
                    byte[] existing = File.ReadAllBytes(secretPath);
                    if (existing.Length == 32)
                        return existing;
                }
                catch
                {
                    // Fallback to regeneration if read fails
                }
            }

            byte[] secret = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(secret);
            }

            try
            {
                File.WriteAllBytes(secretPath, secret);
            }
            catch
            {
                // Fallback ignore write error in restricted environment
            }

            return secret;
        }
    }

    private static byte[] DeriveKeyPbkdf2(byte[] salt)
    {
        byte[] secretBytes = GetOrGenerateFallbackSecret();
        string baseSecret = Convert.ToBase64String(secretBytes);
        string password = $"{baseSecret}_{Environment.MachineName}_{Environment.UserName}_NodeRadarPro_Pbkdf2Secret";
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
    }

    internal static byte[] DeriveKeyPbkdf2Legacy(byte[] salt)
    {
        string password = $"{Environment.MachineName}_{Environment.UserName}_NodeRadarPro_Pbkdf2Secret";
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
    }

    private static byte[] GetFallbackEncryptionKeyLegacy()
    {
        string identifier = $"{Environment.MachineName}_{Environment.UserName}_NodeRadarPro_FallbackKey";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
    }
}
