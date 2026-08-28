using System.Security.Cryptography;
using System.Text;
using NodeRadarPro.Core;

namespace Core.Tests;

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
        // Arrange
        string tempFile = Path.GetTempFileName();
        string testContent = "This is a test content for hashing.";

        string expectedHash;
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(testContent));
            expectedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

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

    [Fact]
    public void VerifyHash_EqualHashes_ReturnsTrue()
    {
        string hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        Assert.True(SecurityService.VerifyHash(hash, hash.ToUpperInvariant()));
    }

    [Fact]
    public void VerifyHash_MismatchedOrNull_ReturnsFalse()
    {
        Assert.False(SecurityService.VerifyHash(null, "abc"));
        Assert.False(SecurityService.VerifyHash("abc", null));
        Assert.False(SecurityService.VerifyHash("abc", "abcd"));
        Assert.False(SecurityService.VerifyHash("abcdef", "123456"));
    }

    [Fact]
    public void VerifyFileHash_ValidFile_ReturnsTrueForMatchingHash()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "hello");
            string? hash = SecurityService.ComputeFileHash(tempFile);
            Assert.NotNull(hash);
            Assert.True(SecurityService.VerifyFileHash(tempFile, hash));
            Assert.False(SecurityService.VerifyFileHash(tempFile, "wronghash"));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
