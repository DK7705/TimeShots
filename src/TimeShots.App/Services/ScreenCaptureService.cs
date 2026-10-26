using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TimeShots.App.Services;

public static class ScreenCaptureService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static Bitmap CaptureFullscreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }
        return bitmap;
    }

    public static Bitmap CaptureActiveWindow()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
        {
            return CaptureFullscreen();
        }

        RECT rect;
        int hr = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf<RECT>());
        if (hr != 0 && !GetWindowRect(hWnd, out rect))
        {
            return CaptureFullscreen();
        }

        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);

        var bounds = SystemInformation.VirtualScreen;
        var targetRect = Rectangle.Intersect(bounds, new Rectangle(rect.Left, rect.Top, width, height));
        if (targetRect.Width <= 0 || targetRect.Height <= 0)
        {
            return CaptureFullscreen();
        }

        var bitmap = new Bitmap(targetRect.Width, targetRect.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(targetRect.Left, targetRect.Top, 0, 0, targetRect.Size, CopyPixelOperation.SourceCopy);
        }
        return bitmap;
    }

    public static Bitmap CaptureRegion(Rectangle region)
    {
        var bounds = SystemInformation.VirtualScreen;
        var clamped = Rectangle.Intersect(bounds, region);
        if (clamped.Width <= 0 || clamped.Height <= 0)
        {
            throw new ArgumentException("Selected region is outside of screen bounds.");
        }

        var bitmap = new Bitmap(clamped.Width, clamped.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(clamped.X, clamped.Y, 0, 0, clamped.Size, CopyPixelOperation.SourceCopy);
        }
        return bitmap;
    }
}
