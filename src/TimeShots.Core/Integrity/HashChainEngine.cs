using System.Security.Cryptography;
using System.Text;

namespace TimeShots.Core.Integrity;

public static class HashChainEngine
{
    public const string GenesisHash = "GENESIS";

    public static string ComputeSha256(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static string ComputeSha256(string text)
    {
        return ComputeSha256(Encoding.UTF8.GetBytes(text));
    }

    public static string ComputeImageHash(byte[] imageBytes)
    {
        return ComputeSha256(imageBytes);
    }

    public static string ComputeChainHash(string currentImageHash, string? previousHash)
    {
        string prev = string.IsNullOrWhiteSpace(previousHash) ? GenesisHash : previousHash;
        string combined = $"{currentImageHash}:{prev}";
        return ComputeSha256(combined);
    }

    public static string ComputeSignature(string chainHash, string timestampIso, byte[] secretKey)
    {
        string payload = $"{chainHash}|{timestampIso}";
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        byte[] hash = HMACSHA256.HashData(secretKey, payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool VerifySignature(string chainHash, string timestampIso, string signatureHex, byte[] secretKey)
    {
        string expectedSignature = ComputeSignature(chainHash, timestampIso, secretKey);
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
        byte[] actualBytes = Encoding.UTF8.GetBytes(signatureHex.ToLowerInvariant());

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
