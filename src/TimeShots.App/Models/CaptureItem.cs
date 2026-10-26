namespace TimeShots.App.Models;

public class CaptureItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Sequence { get; set; }
    public string UtcTimestamp { get; set; } = string.Empty;
    public string LocalTimestamp { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public string ImageHash { get; set; } = string.Empty;
    public string ChainHash { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public string Mode { get; set; } = "fullscreen";
    public string ImagePath { get; set; } = string.Empty;
    public string MetadataPath { get; set; } = string.Empty;
    public bool? IsVerified { get; set; } = null;

    public string FormattedTimestamp
    {
        get
        {
            if (DateTime.TryParse(LocalTimestamp, out var dt))
            {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return LocalTimestamp;
        }
    }
}
