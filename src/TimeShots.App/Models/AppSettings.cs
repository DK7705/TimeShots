namespace TimeShots.App.Models;

public class AppSettings
{
    public string HotkeyFullscreen { get; set; } = "Ctrl+Shift+S";
    public string HotkeyWindow { get; set; } = "Ctrl+Shift+W";
    public string HotkeyRegion { get; set; } = "Ctrl+Shift+R";
    public string HotkeyGallery { get; set; } = "Ctrl+Shift+G";
    public string StoragePath { get; set; } = string.Empty;
    public string NtpServer { get; set; } = "pool.ntp.org";
    public string OverlayStyle { get; set; } = "default";
}
