using System.IO;
using System.Security.Cryptography;
using System.Text;
using NodeRadarPro.Data;

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
        Assert.True(File.Exists(keyFile));
    }

    [Fact]
    public void GetOrGenerateDbPassword_ReadsExistingKey()
    {
        string passwordFirst = CredentialVault.GetOrGenerateDbPassword(_tempFolder);
        string passwordSecond = CredentialVault.GetOrGenerateDbPassword(_tempFolder);

        Assert.Equal(passwordFirst, passwordSecond);
    }

    [Fact]
    public void GetOrGenerateFallbackSecret_CreatesAndPersistsKey()
    {
        byte[] secret1 = CredentialVault.GetOrGenerateFallbackSecret();
        Assert.NotNull(secret1);
        Assert.Equal(32, secret1.Length);

        byte[] secret2 = CredentialVault.GetOrGenerateFallbackSecret();
        Assert.Equal(secret1, secret2);
    }

    [Fact]
    public void DecryptSecret_Version02_LegacyFallback_Succeeds()
    {
        string plainText = "LegacySecretPassword456!@#";
        byte[] salt = new byte[16];
        byte[] nonce = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
            rng.GetBytes(nonce);
        }

        byte[] key = CredentialVault.DeriveKeyPbkdf2Legacy(salt);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherText = new byte[plainBytes.Length];
        byte[] tag = new byte[16];

        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
        }

        byte[] result = new byte[1 + 16 + 12 + 16 + cipherText.Length];
        result[0] = 0x02;
        Buffer.BlockCopy(salt, 0, result, 1, 16);
        Buffer.BlockCopy(nonce, 0, result, 17, 12);
        Buffer.BlockCopy(tag, 0, result, 29, 16);
        Buffer.BlockCopy(cipherText, 0, result, 45, cipherText.Length);

        string legacyEncryptedBase64 = Convert.ToBase64String(result);
        string decrypted = CredentialVault.DecryptSecret(legacyEncryptedBase64);

        Assert.Equal(plainText, decrypted);
    }
}
