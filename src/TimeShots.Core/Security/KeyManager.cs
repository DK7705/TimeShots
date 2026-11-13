using System.Security.Cryptography;
using System.Text;

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
        var value = hex.Trim();
        if (value.Length != KeySizeInBytes * 2)
        {
            throw new ArgumentException($"A key must contain exactly {KeySizeInBytes * 2} hexadecimal characters.", nameof(hex));
        }

        return Convert.FromHexString(value);
    }

    public static byte[] LoadKey(string keyFilePath)
    {
        var keyBytes = File.ReadAllBytes(keyFilePath);
        if (keyBytes.Length == KeySizeInBytes)
        {
            return keyBytes;
        }

        var content = Encoding.ASCII.GetString(keyBytes).Trim();
        try
        {
            return HexToKey(content);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException($"The key file '{keyFilePath}' must contain a 256-bit binary key or 64 hexadecimal characters.", ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"The key file '{keyFilePath}' must contain a 256-bit binary key or 64 hexadecimal characters.", ex);
        }
    }

    public static byte[] GetOrCreateKey(string keyFilePath)
    {
        if (File.Exists(keyFilePath))
        {
            return LoadKey(keyFilePath);
        }

        var dir = Path.GetDirectoryName(keyFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var newKey = GenerateKey();
        try
        {
            using var stream = new FileStream(keyFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var encodedKey = Encoding.ASCII.GetBytes(KeyToHex(newKey));
            stream.Write(encodedKey, 0, encodedKey.Length);
            return newKey;
        }
        catch (IOException) when (File.Exists(keyFilePath))
        {
            return LoadKey(keyFilePath);
        }
    }
}
