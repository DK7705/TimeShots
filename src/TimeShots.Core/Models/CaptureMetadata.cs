using System.Text.Json;
using System.Text.Json.Serialization;

namespace TimeShots.Core.Models;

public class CaptureMetadata
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("capture_id")]
    public string CaptureId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    [JsonPropertyName("timestamps")]
    public TimestampsInfo Timestamps { get; set; } = new();

    [JsonPropertyName("capture")]
    public CaptureInfo Capture { get; set; } = new();

    [JsonPropertyName("integrity")]
    public IntegrityInfo Integrity { get; set; } = new();

    [JsonPropertyName("system")]
    public SystemInfo System { get; set; } = new();

    [JsonPropertyName("files")]
    public FilesInfo Files { get; set; } = new();

    public string ToJson(bool indented = true)
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public static CaptureMetadata? FromJson(string json)
    {
        return JsonSerializer.Deserialize<CaptureMetadata>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}

public class TimestampsInfo
{
    [JsonPropertyName("utc")]
    public string Utc { get; set; } = string.Empty;

    [JsonPropertyName("local")]
    public string Local { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("ntp_offset_ms")]
    public long? NtpOffsetMs { get; set; }

    [JsonPropertyName("ntp_server")]
    public string? NtpServer { get; set; }

    [JsonPropertyName("monotonic_ticks")]
    public long MonotonicTicks { get; set; }
}

public class CaptureInfo
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "fullscreen";

    [JsonPropertyName("display")]
    public int Display { get; set; } = 0;

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = string.Empty;

    [JsonPropertyName("scale_factor")]
    public double ScaleFactor { get; set; } = 1.0;
}

public class IntegrityInfo
{
    [JsonPropertyName("image_hash_sha256")]
    public string ImageHashSha256 { get; set; } = string.Empty;

    [JsonPropertyName("previous_hash")]
    public string PreviousHash { get; set; } = string.Empty;

    [JsonPropertyName("chain_hash")]
    public string ChainHash { get; set; } = string.Empty;

    [JsonPropertyName("hmac_sha256")]
    public string HmacSha256 { get; set; } = string.Empty;

    [JsonPropertyName("chain_position")]
    public long ChainPosition { get; set; }

    [JsonPropertyName("chain_id")]
    public string ChainId { get; set; } = string.Empty;
}

public class SystemInfo
{
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = Environment.MachineName;

    [JsonPropertyName("os")]
    public string Os { get; set; } = Environment.OSVersion.Platform.ToString();

    [JsonPropertyName("os_version")]
    public string OsVersion { get; set; } = Environment.OSVersion.VersionString;

    [JsonPropertyName("username")]
    public string Username { get; set; } = Environment.UserName;

    [JsonPropertyName("app_version")]
    public string AppVersion { get; set; } = "1.0.0";
}

public class FilesInfo
{
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public string Metadata { get; set; } = string.Empty;
}
