using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using TimeShots.App.Models;
using TimeShots.Core.Integrity;
using TimeShots.Core.Models;
using TimeShots.Core.Overlay;
using TimeShots.Core.Security;

namespace TimeShots.App.Services;

public sealed class CaptureStorageService
{
    private readonly AppSettings _settings;

    public CaptureStorageService(AppSettings settings)
    {
        _settings = settings;
    }

    public CaptureMetadata SaveCapture(Bitmap bitmap, string mode)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (string.IsNullOrWhiteSpace(_settings.StoragePath))
        {
            throw new InvalidOperationException("The capture storage path is not configured.");
        }

        Directory.CreateDirectory(_settings.StoragePath);
        var previousCaptures = LoadMetadata();
        var lastCapture = previousCaptures.OrderBy(capture => capture.Sequence).LastOrDefault();
        var sequence = lastCapture == null ? 1 : lastCapture.Sequence + 1;
        var previousHash = lastCapture?.Integrity.ChainHash ?? HashChainEngine.GenesisHash;
        var chainId = lastCapture?.Integrity.ChainId;
        if (string.IsNullOrWhiteSpace(chainId))
        {
            chainId = Guid.NewGuid().ToString("N");
        }

        var timestamp = DateTimeOffset.UtcNow;
        var utc = timestamp.ToString("O", CultureInfo.InvariantCulture);
        var local = timestamp.ToLocalTime();

        byte[] sourceBytes;
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png);
            sourceBytes = stream.ToArray();
        }

        var imageBytes = TimestampOverlayRenderer.RenderOverlay(
            sourceBytes,
            utc,
            local.ToString("O", CultureInfo.InvariantCulture),
            sequence,
            string.Empty,
            _settings.OverlayStyle);

        var imageHash = HashChainEngine.ComputeImageHash(imageBytes);
        var chainHash = HashChainEngine.ComputeChainHash(imageHash, previousHash);
        var key = KeyManager.GetOrCreateKey(_settings.SigningKeyPath);
        var captureId = Guid.NewGuid().ToString("N");
        var imageFileName = $"capture_{timestamp:yyyyMMdd_HHmmss_fff}_{captureId}.png";
        var metadataFileName = Path.ChangeExtension(imageFileName, ".json");

        var metadata = new CaptureMetadata
        {
            CaptureId = captureId,
            Sequence = sequence,
            Timestamps = new TimestampsInfo
            {
                Utc = utc,
                Local = local.ToString("O", CultureInfo.InvariantCulture),
                Timezone = TimeZoneInfo.Local.Id
            },
            Capture = new CaptureInfo
            {
                Mode = mode,
                Resolution = $"{bitmap.Width}x{bitmap.Height}"
            },
            Integrity = new IntegrityInfo
            {
                ImageHashSha256 = imageHash,
                PreviousHash = previousHash,
                ChainHash = chainHash,
                HmacSha256 = HashChainEngine.ComputeSignature(chainHash, utc, key),
                ChainPosition = sequence,
                ChainId = chainId
            },
            Files = new FilesInfo
            {
                Image = imageFileName,
                Metadata = metadataFileName
            }
        };

        var imagePath = Path.Combine(_settings.StoragePath, imageFileName);
        var metadataPath = Path.Combine(_settings.StoragePath, metadataFileName);
        File.WriteAllBytes(imagePath, imageBytes);
        try
        {
            File.WriteAllText(metadataPath, metadata.ToJson());
        }
        catch
        {
            File.Delete(imagePath);
            throw;
        }

        return metadata;
    }

    public IReadOnlyList<CaptureItem> LoadCaptures()
    {
        if (!Directory.Exists(_settings.StoragePath))
        {
            return Array.Empty<CaptureItem>();
        }

        return LoadMetadata()
            .OrderByDescending(capture => capture.Sequence)
            .Select(capture =>
            {
                var metadataFileName = capture.Files.Metadata;
                var metadataPath = string.IsNullOrWhiteSpace(metadataFileName)
                    ? Path.Combine(_settings.StoragePath, $"capture_{capture.CaptureId}.json")
                    : Path.Combine(_settings.StoragePath, metadataFileName);
                var imagePath = string.IsNullOrWhiteSpace(capture.Files.Image)
                    ? Path.ChangeExtension(metadataPath, ".png")
                    : Path.Combine(_settings.StoragePath, capture.Files.Image);

                return new CaptureItem
                {
                    Id = capture.CaptureId,
                    Sequence = capture.Sequence,
                    UtcTimestamp = capture.Timestamps.Utc,
                    LocalTimestamp = capture.Timestamps.Local,
                    Timezone = capture.Timestamps.Timezone,
                    ImageHash = capture.Integrity.ImageHashSha256,
                    ChainHash = capture.Integrity.ChainHash,
                    Resolution = capture.Capture.Resolution,
                    Mode = capture.Capture.Mode,
                    ImagePath = imagePath,
                    MetadataPath = metadataPath
                };
            })
            .ToList();
    }

    public VerificationResult VerifyCapture(CaptureItem item)
    {
        var metadata = CaptureMetadata.FromJson(File.ReadAllText(item.MetadataPath))
            ?? throw new InvalidDataException($"Could not parse metadata file '{item.MetadataPath}'.");
        var imageBytes = File.ReadAllBytes(item.ImagePath);
        var key = File.Exists(_settings.SigningKeyPath)
            ? KeyManager.LoadKey(_settings.SigningKeyPath)
            : null;

        return Verifier.VerifyCapture(imageBytes, metadata, key);
    }

    private List<CaptureMetadata> LoadMetadata()
    {
        if (!Directory.Exists(_settings.StoragePath))
        {
            return new List<CaptureMetadata>();
        }

        var captures = new List<CaptureMetadata>();
        foreach (var file in Directory.GetFiles(_settings.StoragePath, "capture_*.json", SearchOption.TopDirectoryOnly))
        {
            var metadata = CaptureMetadata.FromJson(File.ReadAllText(file));
            if (metadata == null ||
                string.IsNullOrWhiteSpace(metadata.CaptureId) ||
                metadata.Timestamps == null ||
                metadata.Integrity == null ||
                metadata.Capture == null ||
                metadata.Files == null)
            {
                throw new InvalidDataException($"Capture metadata file '{file}' is incomplete.");
            }

            captures.Add(metadata);
        }

        return captures;
    }
}
