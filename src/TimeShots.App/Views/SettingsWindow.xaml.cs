using System.IO;
using System.Windows;
using System.Windows.Forms;
using TimeShots.App.Models;

namespace TimeShots.App.Views;

public partial class SettingsWindow : Window
{
    private AppSettings _settings = new();

    public SettingsWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        TxtStoragePath.Text = _settings.StoragePath;
        CmbOverlayStyle.SelectedIndex = _settings.OverlayStyle switch
        {
            "compact" => 1,
            "minimal" => 2,
            _ => 0
        };
    }

    private void OnBrowseStorageClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select TimeShots storage folder",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtStoragePath.Text = dialog.SelectedPath;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtStoragePath.Text))
        {
            System.Windows.MessageBox.Show("Select a capture storage folder before saving.", "TimeShots", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var updatedSettings = new AppSettings
            {
                HotkeyFullscreen = _settings.HotkeyFullscreen,
                HotkeyWindow = _settings.HotkeyWindow,
                HotkeyRegion = _settings.HotkeyRegion,
                HotkeyGallery = _settings.HotkeyGallery,
                StoragePath = Path.GetFullPath(TxtStoragePath.Text.Trim()),
                SigningKeyPath = _settings.SigningKeyPath,
                NtpServer = _settings.NtpServer,
                OverlayStyle = CmbOverlayStyle.SelectedIndex switch
                {
                    1 => "compact",
                    2 => "minimal",
                    _ => "default"
                }
            };
            updatedSettings.Save();
            _settings = updatedSettings;
            TxtStatus.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Could not save settings: {ex.Message}", "TimeShots", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
