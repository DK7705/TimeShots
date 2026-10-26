using System.Security.Cryptography;

namespace TimeShots.Core.Security;

public static class KeyManager
{
    public const int KeySizeInBytes = 32;

    public static byte[] GenerateKey()
    {
        return RandomNumberGenerator.GetBytes(KeySizeInBytes);
    }

    public static string KeyToHex(byte[] key)
    {
        return Convert.ToHexString(key).ToLowerInvariant();
    }

    public static byte[] HexToKey(string hex)
    {
        return Convert.FromHexString(hex.Trim());
    }

    public static byte[] GetOrCreateKey(string keyFilePath)
    {
        if (File.Exists(keyFilePath))
        {
            var content = File.ReadAllText(keyFilePath).Trim();
            if (content.Length == KeySizeInBytes * 2)
            {
                return HexToKey(content);
            }
            // Raw binary key fallback
            var rawBytes = File.ReadAllBytes(keyFilePath);
            if (rawBytes.Length == KeySizeInBytes)
            {
                return rawBytes;
            }
        }

        var dir = Path.GetDirectoryName(keyFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var newKey = GenerateKey();
        File.WriteAllText(keyFilePath, KeyToHex(newKey));
        return newKey;
    }
}
