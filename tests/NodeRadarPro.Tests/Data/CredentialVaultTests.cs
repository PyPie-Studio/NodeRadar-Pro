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
}
