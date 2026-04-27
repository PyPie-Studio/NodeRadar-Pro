using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NodeRadarPro.Core
{
    /// <summary>
    /// Provides security-related utility functions, such as hashing.
    /// </summary>
    public static class SecurityService
    {
        /// <summary>
        /// Computes the SHA256 hash of a file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>A lowercase hex string representing the SHA256 hash, or null if the file does not exist.</returns>
        public static string ComputeFileHash(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    
                    return sb.ToString();
                }
            }
        }
    }
}
