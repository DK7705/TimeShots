using System.IO;
using System.Text.Json;

namespace TimeShots.App.Models;

public class AppSettings
{
    public string HotkeyFullscreen { get; set; } = "Ctrl+Shift+S";
    public string HotkeyWindow { get; set; } = "Ctrl+Shift+W";
    public string HotkeyRegion { get; set; } = "Ctrl+Shift+R";
    public string HotkeyGallery { get; set; } = "Ctrl+Shift+G";
    public string StoragePath { get; set; } = DefaultStoragePath;
    public string SigningKeyPath { get; set; } = DefaultSigningKeyPath;
    public string NtpServer { get; set; } = "pool.ntp.org";
    public string OverlayStyle { get; set; } = "default";

    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimeShots");

    public static string SettingsFilePath => Path.Combine(AppDataDirectory, "settings.json");
    public static string DefaultSigningKeyPath => Path.Combine(AppDataDirectory, "signing.key");
    public static string DefaultStoragePath => Path.Combine(AppDataDirectory, "captures");

    public static AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new AppSettings();
        }

        var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFilePath));
        if (settings == null)
        {
            throw new InvalidDataException($"Settings file '{SettingsFilePath}' is empty or invalid.");
        }

        if (string.IsNullOrWhiteSpace(settings.StoragePath))
        {
            settings.StoragePath = DefaultStoragePath;
        }

        if (string.IsNullOrWhiteSpace(settings.SigningKeyPath))
        {
            settings.SigningKeyPath = DefaultSigningKeyPath;
        }

        if (string.IsNullOrWhiteSpace(settings.NtpServer))
        {
            settings.NtpServer = "pool.ntp.org";
        }

        return settings;
    }

    public void Save()
    {
        Directory.CreateDirectory(AppDataDirectory);
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}
