using System.Drawing;
using System.Windows.Forms;

namespace TimeShots.App.Services;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event Action? CaptureFullscreenRequested;
    public event Action? CaptureWindowRequested;
    public event Action? CaptureRegionRequested;
    public event Action? GalleryRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;

    public TrayIconManager()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "TimeShots",
            Visible = true,
            Icon = SystemIcons.Application
        };

        var contextMenu = new ContextMenuStrip();

        var captureFullscreenItem = new ToolStripMenuItem("📸  Capture Fullscreen (Ctrl+Shift+S)", null, (_, _) => CaptureFullscreenRequested?.Invoke());
        var captureWindowItem = new ToolStripMenuItem("🪟  Capture Window (Ctrl+Shift+W)", null, (_, _) => CaptureWindowRequested?.Invoke());
        var captureRegionItem = new ToolStripMenuItem("✂️  Capture Region (Ctrl+Shift+R)", null, (_, _) => CaptureRegionRequested?.Invoke());
        var galleryItem = new ToolStripMenuItem("🖼️  Gallery (Ctrl+Shift+G)", null, (_, _) => GalleryRequested?.Invoke());
        var settingsItem = new ToolStripMenuItem("⚙️  Settings", null, (_, _) => SettingsRequested?.Invoke());
        var exitItem = new ToolStripMenuItem("❌  Exit", null, (_, _) => ExitRequested?.Invoke());

        contextMenu.Items.Add(captureFullscreenItem);
        contextMenu.Items.Add(captureWindowItem);
        contextMenu.Items.Add(captureRegionItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(galleryItem);
        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => GalleryRequested?.Invoke();
    }

    public void ShowNotification(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, text, icon);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        GC.SuppressFinalize(this);
    }
}
