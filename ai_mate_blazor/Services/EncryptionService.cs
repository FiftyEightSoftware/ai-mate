using System.Security.Cryptography;
using System.Text;

namespace ai_mate_blazor.Services;

public class EncryptionService
{
    // Simple XOR-based obfuscation for client-side storage
    // For production, consider using more robust encryption with a user-specific key
    private const string DefaultKey = "AIMate2024SecureKey!@#$";

    public string Encrypt(string? plainText, string? customKey = null)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return string.Empty;
        
        var key = customKey ?? DefaultKey;
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var encrypted = new byte[plainBytes.Length];

        for (int i = 0; i < plainBytes.Length; i++)
        {
            encrypted[i] = (byte)(plainBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return Convert.ToBase64String(encrypted);
    }

    public string? Decrypt(string? encryptedText, string? customKey = null)
    {
        if (string.IsNullOrWhiteSpace(encryptedText)) return null;
        
        try
        {
            var key = customKey ?? DefaultKey;
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var decrypted = new byte[encryptedBytes.Length];

            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decrypted[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return null;
        }
    }
}
