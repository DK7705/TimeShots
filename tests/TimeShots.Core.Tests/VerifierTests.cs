using TimeShots.Core.Integrity;
using TimeShots.Core.Models;
using TimeShots.Core.Security;
using Xunit;

namespace TimeShots.Core.Tests;

public class VerifierTests
{
    private static readonly byte[] Key = Enumerable.Range(0, KeyManager.KeySizeInBytes).Select(value => (byte)value).ToArray();

    [Fact]
    public void VerifyCapture_RequiresSigningKey()
    {
        var image = new byte[] { 1, 2, 3 };
        var metadata = CreateCapture(image, 1, HashChainEngine.GenesisHash, DateTimeOffset.UtcNow.ToString("O"));

        var result = Verifier.VerifyCapture(image, metadata);

        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, check => check.Name == "HMAC-SHA256 Signature" && !check.Passed);
    }

    [Fact]
    public void VerifyCapture_AcceptsValidHashAndSignature()
    {
        var image = new byte[] { 1, 2, 3 };
        var metadata = CreateCapture(image, 1, HashChainEngine.GenesisHash, DateTimeOffset.UtcNow.ToString("O"));

        var result = Verifier.VerifyCapture(image, metadata, Key);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void VerifyChain_RejectsMalformedUtcTimestamp()
    {
        var metadata = CreateCapture(new byte[] { 1, 2, 3 }, 1, HashChainEngine.GenesisHash, "not-a-timestamp");

        var result = Verifier.VerifyChain(new[] { metadata }, Key);

        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, check => check.Name == "Timestamp Ordering" && !check.Passed);
    }

    [Fact]
    public void VerifyChain_RequiresGenesisSequenceAndLink()
    {
        var metadata = CreateCapture(new byte[] { 1, 2, 3 }, 4, "unverified-prefix", DateTimeOffset.UtcNow.ToString("O"));

        var result = Verifier.VerifyChain(new[] { metadata }, Key);

        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, check => check.Name == "Sequence Monotonicity" && !check.Passed);
        Assert.Contains(result.Checks, check => check.Name == "Chain Hash Linkage" && !check.Passed);
    }

    [Fact]
    public void VerifyChain_AcceptsValidGenesisCapture()
    {
        var metadata = CreateCapture(new byte[] { 1, 2, 3 }, 1, HashChainEngine.GenesisHash, DateTimeOffset.UtcNow.ToString("O"));

        var result = Verifier.VerifyChain(new[] { metadata }, Key);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void VerifyChain_RequiresSigningKey()
    {
        var metadata = CreateCapture(new byte[] { 1, 2, 3 }, 1, HashChainEngine.GenesisHash, DateTimeOffset.UtcNow.ToString("O"));

        var result = Verifier.VerifyChain(new[] { metadata });

        Assert.False(result.IsValid);
        Assert.Contains(result.Checks, check => check.Name == "HMAC Signatures in Chain" && !check.Passed);
    }

    private static CaptureMetadata CreateCapture(byte[] image, long sequence, string previousHash, string utc)
    {
        var imageHash = HashChainEngine.ComputeImageHash(image);
        var chainHash = HashChainEngine.ComputeChainHash(imageHash, previousHash);
        return new CaptureMetadata
        {
            Sequence = sequence,
            Timestamps = new TimestampsInfo { Utc = utc },
            Integrity = new IntegrityInfo
            {
                ImageHashSha256 = imageHash,
                PreviousHash = previousHash,
                ChainHash = chainHash,
                HmacSha256 = HashChainEngine.ComputeSignature(chainHash, utc, Key)
            }
        };
    }
}

public class KeyManagerTests
{
    [Fact]
    public void GetOrCreateKey_DoesNotReplaceInvalidExistingFile()
    {
        var keyPath = Path.Combine(Path.GetTempPath(), $"timeshots-{Guid.NewGuid():N}.key");
        var originalContent = System.Text.Encoding.UTF8.GetBytes("invalid key material");
        File.WriteAllBytes(keyPath, originalContent);

        try
        {
            Assert.Throws<InvalidDataException>(() => KeyManager.GetOrCreateKey(keyPath));
            Assert.Equal(originalContent, File.ReadAllBytes(keyPath));
        }
        finally
        {
            File.Delete(keyPath);
        }
    }
}
