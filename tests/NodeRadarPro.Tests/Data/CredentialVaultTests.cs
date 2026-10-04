using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NodeRadarPro.Data;
using Xunit;

namespace NodeRadarPro.Tests;

public class CredentialVaultTests : IDisposable
{
    private readonly string _tempFolder;

    public CredentialVaultTests()
    {
        _tempFolder = Path.Combine(Path.GetTempPath(), "NodeRadarVaultTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempFolder))
                Directory.Delete(_tempFolder, true);
        }
        catch
        {
            // Ignore cleanup exceptions
        }
    }

    [Fact]
    public void EncryptSecret_RoundTrips_Correctly()
    {
        string secret = "SuperSecretPassword123!@#";
        string encrypted = CredentialVault.EncryptSecret(secret);

        Assert.NotEmpty(encrypted);
        Assert.NotEqual(secret, encrypted);

        string decrypted = CredentialVault.DecryptSecret(encrypted);
        Assert.Equal(secret, decrypted);
    }

    [Fact]
    public void EncryptSecret_EmptyOrNullInput_ReturnsEmpty()
    {
        Assert.Equal("", CredentialVault.EncryptSecret(""));
        Assert.Equal("", CredentialVault.EncryptSecret(null!));
    }

    [Fact]
    public void DecryptSecret_EmptyOrInvalidInput_ReturnsEmpty()
    {
        Assert.Equal("", CredentialVault.DecryptSecret(""));
        Assert.Equal("", CredentialVault.DecryptSecret(null!));
        Assert.Equal("", CredentialVault.DecryptSecret("NotValidBase64!!!"));
    }

    [Fact]
    public void GetOrGenerateDbPassword_CreatesKeyFile()
    {
        string keyFile = Path.Combine(_tempFolder, "db_key.bin");
        Assert.False(File.Exists(keyFile));

        string password = CredentialVault.GetOrGenerateDbPassword(_tempFolder);

        Assert.NotEmpty(password);
        if (Environment.GetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING") == "true")
        {
            Assert.Equal("test_password", password);
        }
        else
        {
            Assert.True(File.Exists(keyFile));
        }
    }

    [Fact]
    public void GetOrGenerateDbPassword_ReadsExistingKey()
    {
        string passwordFirst = CredentialVault.GetOrGenerateDbPassword(_tempFolder);
        string passwordSecond = CredentialVault.GetOrGenerateDbPassword(_tempFolder);

        Assert.Equal(passwordFirst, passwordSecond);
    }

    [Fact]
    public void GetOrCreateFallbackMasterSecret_Returns32BytesAndPersists()
    {
        byte[] secret1 = CredentialVault.GetOrCreateFallbackMasterSecret();
        Assert.NotNull(secret1);
        Assert.Equal(32, secret1.Length);

        byte[] secret2 = CredentialVault.GetOrCreateFallbackMasterSecret();
        Assert.Equal(secret1, secret2);
    }

    [Fact]
    public void DecryptSecret_Version01Legacy_DecryptsCorrectly()
    {
        // Construct a v0.01 payload encrypted using GetFallbackEncryptionKeyLegacy
        byte[] key = CredentialVault.GetFallbackEncryptionKeyLegacy();
        byte[] nonce = new byte[12];
        byte[] tag = new byte[16];
        byte[] plainBytes = Encoding.UTF8.GetBytes("LegacySecret123");
        byte[] cipherText = new byte[plainBytes.Length];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(nonce);
        }

        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
        }

        byte[] payload = new byte[1 + 12 + 16 + cipherText.Length];
        payload[0] = 0x01;
        Buffer.BlockCopy(nonce, 0, payload, 1, 12);
        Buffer.BlockCopy(tag, 0, payload, 13, 16);
        Buffer.BlockCopy(cipherText, 0, payload, 29, cipherText.Length);

        string encryptedBase64 = Convert.ToBase64String(payload);
        string decrypted = CredentialVault.DecryptSecret(encryptedBase64);

        Assert.Equal("LegacySecret123", decrypted);
    }
}
