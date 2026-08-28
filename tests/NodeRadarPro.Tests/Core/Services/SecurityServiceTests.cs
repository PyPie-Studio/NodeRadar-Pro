using System.Security.Cryptography;
using System.Text;
using NodeRadarPro.Core;

namespace NodeRadarPro.Tests;

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
        string expectedHash;
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(testContent));
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            expectedHash = sb.ToString();
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
}
