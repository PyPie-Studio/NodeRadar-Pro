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
    public void ProtectData_UnprotectData_RoundTrips_Successfully()
    {
        byte[] input = Encoding.UTF8.GetBytes("ArbitraryRawBytePayload!@#123");
        byte[] protectedBytes = CredentialVault.ProtectData(input);

        Assert.NotNull(protectedBytes);
        Assert.NotEmpty(protectedBytes);

        byte[] unprotectedBytes = CredentialVault.UnprotectData(protectedBytes);
        Assert.Equal(input, unprotectedBytes);
    }

    [Fact]
    public void SaveDbPassword_PersistsKeyFileAndAllowsRetrieval()
    {
        string customPassword = "CustomGeneratedAdminPassword$99!";
        CredentialVault.SaveDbPassword(_tempFolder, customPassword);

        string retrievedPassword = CredentialVault.GetOrGenerateDbPassword(_tempFolder);
        Assert.Equal(customPassword, retrievedPassword);
    }

    [Fact]
    public void MockDpapiEnvVar_DoesNotReturnHardcodedTestPassword()
    {
        string? originalEnv = Environment.GetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING");
        try
        {
            Environment.SetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING", "true");

            string separateFolder = Path.Combine(Path.GetTempPath(), "NodeRadarVaultTest_MockEnv_" + Guid.NewGuid());
            Directory.CreateDirectory(separateFolder);
            try
            {
                string password = CredentialVault.GetOrGenerateDbPassword(separateFolder);
                Assert.NotEqual("test_password", password);
                Assert.True(File.Exists(Path.Combine(separateFolder, "db_key.bin")));
            }
            finally
            {
                if (Directory.Exists(separateFolder))
                    Directory.Delete(separateFolder, true);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOCK_DPAPI_FOR_TESTING", originalEnv);
        }
    }

    [Fact]
    public void UnprotectData_WithLegacyV1Payload_DecryptsCorrectly()
    {
        byte[] key = CredentialVault.GetFallbackEncryptionKeyLegacy();
        byte[] nonce = new byte[12];
        byte[] tag = new byte[16];
        byte[] plainBytes = Encoding.UTF8.GetBytes("LegacyV1SecretPayload");
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

        byte[] decrypted = CredentialVault.UnprotectData(payload);
        Assert.Equal("LegacyV1SecretPayload", Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public void UnprotectData_WithV2Payload_DecryptsCorrectly()
    {
        byte[] salt = new byte[16];
        byte[] nonce = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
            rng.GetBytes(nonce);
        }

        byte[] key = CredentialVault.DeriveKeyPbkdf2(salt);
        byte[] plainBytes = Encoding.UTF8.GetBytes("ModernV2SecretPayload");
        byte[] cipherText = new byte[plainBytes.Length];
        byte[] tag = new byte[16];

        using (var aesGcm = new AesGcm(key, 16))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
        }

        byte[] payload = new byte[1 + 16 + 12 + 16 + cipherText.Length];
        payload[0] = 0x02;
        Buffer.BlockCopy(salt, 0, payload, 1, 16);
        Buffer.BlockCopy(nonce, 0, payload, 17, 12);
        Buffer.BlockCopy(tag, 0, payload, 29, 16);
        Buffer.BlockCopy(cipherText, 0, payload, 45, cipherText.Length);

        byte[] decrypted = CredentialVault.UnprotectData(payload);
        Assert.Equal("ModernV2SecretPayload", Encoding.UTF8.GetString(decrypted));
    }

    [Fact]
    public void UnprotectData_WithCorruptedPayload_ReturnsDataWithoutThrowing()
    {
        byte[] corruptPayload = new byte[50];
        corruptPayload[0] = 0x02; // Claims v2 format but has random garbage
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(corruptPayload.AsSpan(1));
        }

        byte[] result = CredentialVault.UnprotectData(corruptPayload);
        Assert.Equal(corruptPayload, result);
    }
}
