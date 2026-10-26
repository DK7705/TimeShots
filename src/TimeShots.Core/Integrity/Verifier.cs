using TimeShots.Core.Models;

namespace TimeShots.Core.Integrity;

public class VerificationCheck
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class VerificationResult
{
    public bool IsValid { get; set; }
    public List<VerificationCheck> Checks { get; set; } = new();
    public string Summary => IsValid ? "All integrity checks passed successfully." : "Integrity check failed.";
}

public static class Verifier
{
    public static VerificationResult VerifyCapture(byte[] imageBytes, CaptureMetadata metadata, byte[]? secretKey = null)
    {
        var result = new VerificationResult();

        // Check 1: Image SHA-256 hash matches metadata
        string actualImageHash = HashChainEngine.ComputeImageHash(imageBytes);
        bool hashMatch = string.Equals(actualImageHash, metadata.Integrity.ImageHashSha256, StringComparison.OrdinalIgnoreCase);
        result.Checks.Add(new VerificationCheck
        {
            Name = "Image Hash (SHA-256)",
            Passed = hashMatch,
            Message = hashMatch
                ? $"Verified: {actualImageHash[..Math.Min(16, actualImageHash.Length)]}..."
                : $"Mismatch! Actual: {actualImageHash}, Expected: {metadata.Integrity.ImageHashSha256}"
        });

        // Check 2: Chain hash formula
        string expectedChainHash = HashChainEngine.ComputeChainHash(metadata.Integrity.ImageHashSha256, metadata.Integrity.PreviousHash);
        bool chainHashMatch = string.Equals(expectedChainHash, metadata.Integrity.ChainHash, StringComparison.OrdinalIgnoreCase);
        result.Checks.Add(new VerificationCheck
        {
            Name = "Chain Link Hash Formula",
            Passed = chainHashMatch,
            Message = chainHashMatch
                ? "Chain linkage calculation is valid."
                : $"Chain hash calculation mismatch! Expected {expectedChainHash}, got {metadata.Integrity.ChainHash}"
        });

        // Check 3: HMAC Signature (if key provided)
        if (secretKey != null && secretKey.Length > 0)
        {
            bool sigMatch = HashChainEngine.VerifySignature(
                metadata.Integrity.ChainHash,
                metadata.Timestamps.Utc,
                metadata.Integrity.HmacSha256,
                secretKey
            );
            result.Checks.Add(new VerificationCheck
            {
                Name = "HMAC-SHA256 Signature",
                Passed = sigMatch,
                Message = sigMatch
                    ? "Cryptographic signature is valid and matches the provided secret key."
                    : "Cryptographic signature verification failed! The key or payload does not match."
            });
        }
        else
        {
            result.Checks.Add(new VerificationCheck
            {
                Name = "HMAC-SHA256 Signature",
                Passed = true,
                Message = "Skipped (no signing key provided for verification)."
            });
        }

        result.IsValid = result.Checks.All(c => c.Passed);
        return result;
    }

    public static VerificationResult VerifyChain(IReadOnlyList<CaptureMetadata> captures, byte[]? secretKey = null)
    {
        var result = new VerificationResult();
        if (captures == null || captures.Count == 0)
        {
            result.Checks.Add(new VerificationCheck
            {
                Name = "Chain Presence",
                Passed = false,
                Message = "No captures provided to verify."
            });
            result.IsValid = false;
            return result;
        }

        var sorted = captures.OrderBy(c => c.Sequence).ToList();

        // 1. Sequence continuity
        bool sequenceValid = true;
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].Sequence != sorted[i - 1].Sequence + 1)
            {
                sequenceValid = false;
                result.Checks.Add(new VerificationCheck
                {
                    Name = "Sequence Monotonicity",
                    Passed = false,
                    Message = $"Sequence gap detected between #{sorted[i - 1].Sequence} and #{sorted[i].Sequence}!"
                });
                break;
            }
        }
        if (sequenceValid)
        {
            result.Checks.Add(new VerificationCheck
            {
                Name = "Sequence Monotonicity",
                Passed = true,
                Message = $"All {sorted.Count} captures follow contiguous sequence numbering."
            });
        }

        // 2. Chain hash formula verification on all captures
        bool formulaValid = true;
        foreach (var item in sorted)
        {
            string expectedHash = HashChainEngine.ComputeChainHash(item.Integrity.ImageHashSha256, item.Integrity.PreviousHash);
            if (!string.Equals(expectedHash, item.Integrity.ChainHash, StringComparison.OrdinalIgnoreCase))
            {
                formulaValid = false;
                result.Checks.Add(new VerificationCheck
                {
                    Name = "Chain Link Hash Formula",
                    Passed = false,
                    Message = $"Invalid chain hash formula on capture #{item.Sequence}! Expected {expectedHash}, got {item.Integrity.ChainHash}"
                });
                break;
            }
        }
        if (formulaValid)
        {
            result.Checks.Add(new VerificationCheck
            {
                Name = "Chain Link Hash Formula",
                Passed = true,
                Message = "All captures correctly satisfy the H_n + C_{n-1} hash chain formula."
            });
        }

        // 3. Chain hash linkage
        bool linkageValid = true;
        for (int i = 1; i < sorted.Count; i++)
        {
            if (!string.Equals(sorted[i].Integrity.PreviousHash, sorted[i - 1].Integrity.ChainHash, StringComparison.OrdinalIgnoreCase))
            {
                linkageValid = false;
                result.Checks.Add(new VerificationCheck
                {
                    Name = "Chain Hash Linkage",
                    Passed = false,
                    Message = $"Broken chain between #{sorted[i - 1].Sequence} and #{sorted[i].Sequence}!"
                });
                break;
            }
        }
        if (linkageValid)
        {
            result.Checks.Add(new VerificationCheck
            {
                Name = "Chain Hash Linkage",
                Passed = true,
                Message = "All captures are unbroken and cryptographically chained together."
            });
        }

        // 4. Monotonic timestamps (UTC)
        bool timeValid = true;
        for (int i = 1; i < sorted.Count; i++)
        {
            if (DateTimeOffset.TryParse(sorted[i].Timestamps.Utc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var curr) &&
                DateTimeOffset.TryParse(sorted[i - 1].Timestamps.Utc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var prev))
            {
                if (curr < prev)
                {
                    timeValid = false;
                    result.Checks.Add(new VerificationCheck
                    {
                        Name = "Timestamp Ordering",
                        Passed = false,
                        Message = $"Time regression! Capture #{sorted[i].Sequence} ({curr:O}) is earlier than #{sorted[i - 1].Sequence} ({prev:O})."
                    });
                    break;
                }
            }
        }
        if (timeValid)
        {
            result.Checks.Add(new VerificationCheck
            {
                Name = "Timestamp Ordering",
                Passed = true,
                Message = "Timestamps are chronologically ascending."
            });
        }

        // 5. Signatures (if key supplied)
        if (secretKey != null && secretKey.Length > 0)
        {
            int sigFailures = 0;
            foreach (var item in sorted)
            {
                if (!HashChainEngine.VerifySignature(item.Integrity.ChainHash, item.Timestamps.Utc, item.Integrity.HmacSha256, secretKey))
                {
                    sigFailures++;
                }
            }

            result.Checks.Add(new VerificationCheck
            {
                Name = "HMAC Signatures in Chain",
                Passed = sigFailures == 0,
                Message = sigFailures == 0
                    ? $"All {sorted.Count} signatures verified against secret key."
                    : $"{sigFailures} captures failed signature validation!"
            });
        }

        result.IsValid = result.Checks.All(c => c.Passed);
        return result;
    }
}
