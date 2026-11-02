using System.Security.Cryptography;
using System.Text;

namespace NodeRadarPro.Tests.Core.Services;

public class SecurityServiceTests
{
    [Fact]
    public void ComputeFileHash_FileDoesNotExist_ReturnsNull()
    {
        // Arrange
        string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        string? result = SecurityService.ComputeFileHash(nonExistentFile);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ComputeFileHash_ValidFile_ReturnsExpectedHash()
    {
        // Arrange - use Path.GetRandomFileName to satisfy S5445
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string testContent = "This is a test content for hashing.";

        // Expected SHA256 Hash
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(testContent));
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        string expectedHash = sb.ToString();

        try
        {
            File.WriteAllText(tempFile, testContent);

            // Act
            string? result = SecurityService.ComputeFileHash(tempFile);

            // Assert
            Assert.Equal(expectedHash, result);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Theory]
    [InlineData(null, "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", null)]
    [InlineData(null, null)]
    public void VerifyHash_NullInputs_ReturnsFalse(string? hashA, string? hashB)
    {
        // Act
        bool result = SecurityService.VerifyHash(hashA, hashB);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("abc", "abcd")]
    [InlineData("12345", "1234")]
    public void VerifyHash_DifferentLengths_ReturnsFalse(string hashA, string hashB)
    {
        // Act
        bool result = SecurityService.VerifyHash(hashA, hashB);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyHash_MatchingHashes_ReturnsTrue()
    {
        // Arrange
        string hashA = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        string hashB = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        bool result = SecurityService.VerifyHash(hashA, hashB);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyHash_CaseInsensitiveMatching_ReturnsTrue()
    {
        // Arrange
        string hashA = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";
        string hashB = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        bool result = SecurityService.VerifyHash(hashA, hashB);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyHash_SameLengthDifferentHashes_ReturnsFalse()
    {
        // Arrange
        string hashA = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        string hashB = "f3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        bool result = SecurityService.VerifyHash(hashA, hashB);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyHash_EmptyStrings_ReturnsTrue()
    {
        // Act
        bool result = SecurityService.VerifyHash("", "");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyFileHash_FileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        string nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        bool result = SecurityService.VerifyFileHash(nonExistentFile, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyFileHash_ValidFileAndMatchingHash_ReturnsTrue()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string testContent = "This is a test content for verifying file hash.";
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(testContent));
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        string expectedHash = sb.ToString();

        try
        {
            File.WriteAllText(tempFile, testContent);

            // Act
            bool result = SecurityService.VerifyFileHash(tempFile, expectedHash);

            // Assert
            Assert.True(result);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void VerifyFileHash_ValidFileAndMismatchingHash_ReturnsFalse()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string testContent = "This is a test content for verifying file hash.";
        string wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

        try
        {
            File.WriteAllText(tempFile, testContent);

            // Act
            bool result = SecurityService.VerifyFileHash(tempFile, wrongHash);

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void VerifySystemIntegrity_InitialRun_LocksSignatureWithVersion()
    {
        using var ms = new MemoryStream();
        using var liteDb = new LiteDB.LiteDatabase(ms);
        var db = new NodeRadarPro.Data.LocalDatabase(liteDb);
        var settings = new AppSettings { LastKnownGoodHash = "INITIAL" };

        SecurityService.VerifySystemIntegrity(settings, db, "2.2.0");

        Assert.NotEmpty(settings.LastKnownGoodHash);
        Assert.StartsWith("2.2.0:", settings.LastKnownGoodHash);
        var logs = db.GetLogs();
        Assert.Contains(logs, l => l.Message.Contains("System integrity signature locked for v2.2.0."));
    }

    [Fact]
    public void VerifySystemIntegrity_MatchingSignature_LogsSuccess()
    {
        using var ms = new MemoryStream();
        using var liteDb = new LiteDB.LiteDatabase(ms);
        var db = new NodeRadarPro.Data.LocalDatabase(liteDb);
        var settings = new AppSettings { LastKnownGoodHash = "INITIAL" };

        SecurityService.VerifySystemIntegrity(settings, db, "2.2.0");

        SecurityService.VerifySystemIntegrity(settings, db, "2.2.0");

        var logs = db.GetLogs();
        Assert.Contains(logs, l => l.Message.Contains("System integrity verified (SHA256)."));
    }

    [Fact]
    public void VerifySystemIntegrity_VersionUpgrade_ReLocksSignatureWithoutWarning()
    {
        using var ms = new MemoryStream();
        using var liteDb = new LiteDB.LiteDatabase(ms);
        var db = new NodeRadarPro.Data.LocalDatabase(liteDb);
        var settings = new AppSettings { LastKnownGoodHash = "INITIAL" };

        SecurityService.VerifySystemIntegrity(settings, db, "2.1.0");

        SecurityService.VerifySystemIntegrity(settings, db, "2.2.0");

        Assert.StartsWith("2.2.0:", settings.LastKnownGoodHash);
        var logs = db.GetLogs();
        Assert.Contains(logs, l => l.Message.Contains("System integrity signature locked for v2.2.0."));
        Assert.DoesNotContain(logs, l => l.Message.Contains("CORE INTEGRITY MISMATCH"));
    }
}
