using System.Threading;
using System.Windows;
using System.Windows.Input;
using TimeShots.App.Models;
using TimeShots.App.Services;
using TimeShots.App.Views;

namespace TimeShots.App;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstanceMutex;
    private TrayIconManager? _trayManager;
    private HotkeyManager? _hotkeyManager;
    private GalleryWindow? _galleryWindow;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "Global\\TimeShotsSingleInstanceMutex";
        _singleInstanceMutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            System.Windows.MessageBox.Show("TimeShots is already running in the system tray.", "TimeShots", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        InitializeTray();
        InitializeHotkeys();
    }

    private void InitializeTray()
    {
        _trayManager = new TrayIconManager();
        _trayManager.CaptureFullscreenRequested += TriggerFullscreenCapture;
        _trayManager.CaptureWindowRequested += TriggerWindowCapture;
        _trayManager.CaptureRegionRequested += TriggerRegionCapture;
        _trayManager.GalleryRequested += OpenGallery;
        _trayManager.SettingsRequested += OpenSettings;
        _trayManager.ExitRequested += ExitApp;
    }

    private void InitializeHotkeys()
    {
        try
        {
            _hotkeyManager = new HotkeyManager();
            _hotkeyManager.Register(Key.S, ModifierKeys.Control | ModifierKeys.Shift, TriggerFullscreenCapture);
            _hotkeyManager.Register(Key.W, ModifierKeys.Control | ModifierKeys.Shift, TriggerWindowCapture);
            _hotkeyManager.Register(Key.R, ModifierKeys.Control | ModifierKeys.Shift, TriggerRegionCapture);
            _hotkeyManager.Register(Key.G, ModifierKeys.Control | ModifierKeys.Shift, OpenGallery);
        }
        catch (Exception ex)
        {
            _trayManager?.ShowNotification("TimeShots Hotkey Error", ex.Message, System.Windows.Forms.ToolTipIcon.Warning);
        }
    }

    private void TriggerFullscreenCapture()
    {
        try
        {
            using var bitmap = ScreenCaptureService.CaptureFullscreen();
            SaveCapture(bitmap, "fullscreen");
        }
        catch (Exception ex)
        {
            _trayManager?.ShowNotification("Capture Error", ex.Message, System.Windows.Forms.ToolTipIcon.Error);
        }
    }

    private void TriggerWindowCapture()
    {
        try
        {
            using var bitmap = ScreenCaptureService.CaptureActiveWindow();
            SaveCapture(bitmap, "window");
        }
        catch (Exception ex)
        {
            _trayManager?.ShowNotification("Capture Error", ex.Message, System.Windows.Forms.ToolTipIcon.Error);
        }
    }

    private void TriggerRegionCapture()
    {
        var overlay = new RegionOverlayWindow();
        overlay.RegionSelected += (region) =>
        {
            try
            {
                using var bitmap = ScreenCaptureService.CaptureRegion(region);
                SaveCapture(bitmap, "region");
            }
            catch (Exception ex)
            {
                _trayManager?.ShowNotification("Capture Error", ex.Message, System.Windows.Forms.ToolTipIcon.Error);
            }
        };
        overlay.Show();
    }

    private void SaveCapture(System.Drawing.Bitmap bitmap, string mode)
    {
        var settings = AppSettings.Load();
        var metadata = new CaptureStorageService(settings).SaveCapture(bitmap, mode);
        _trayManager?.ShowNotification(
            "TimeShots",
            $"Capture #{metadata.Sequence} saved to {settings.StoragePath}.");
    }

    private void OpenGallery()
    {
        if (_galleryWindow == null || !_galleryWindow.IsLoaded)
        {
            _galleryWindow = new GalleryWindow();
            _galleryWindow.Closed += (_, _) => _galleryWindow = null;
        }

        _galleryWindow.Show();
        _galleryWindow.Activate();
    }

    private void OpenSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ExitApp()
    {
        _hotkeyManager?.Dispose();
        _trayManager?.Dispose();
        _singleInstanceMutex?.ReleaseMutex();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _trayManager?.Dispose();
        base.OnExit(e);
    }
}
