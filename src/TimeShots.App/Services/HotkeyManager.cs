using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace TimeShots.App.Services;

public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;

    public const uint MOD_NONE = 0x0000;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();
    private int _currentId = 1000;

    public HotkeyManager()
    {
        var parameters = new HwndSourceParameters("TimeShotsHotkeyListener")
        {
            WindowStyle = 0,
            ExtendedWindowStyle = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE
        };
        _source = new HwndSource(parameters);
        _hwnd = _source.Handle;
        _source.AddHook(HwndHook);
    }

    public int Register(Key key, ModifierKeys modifiers, Action callback)
    {
        int id = ++_currentId;
        uint mod = MOD_NOREPEAT;
        if ((modifiers & ModifierKeys.Alt) != 0) mod |= MOD_ALT;
        if ((modifiers & ModifierKeys.Control) != 0) mod |= MOD_CONTROL;
        if ((modifiers & ModifierKeys.Shift) != 0) mod |= MOD_SHIFT;
        if ((modifiers & ModifierKeys.Windows) != 0) mod |= MOD_WIN;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (RegisterHotKey(_hwnd, id, mod, vk))
        {
            _actions[id] = callback;
            return id;
        }

        return -1;
    }

    public void Unregister(int id)
    {
        if (_actions.Remove(id))
        {
            UnregisterHotKey(_hwnd, id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _actions.Keys.ToList())
        {
            UnregisterHotKey(_hwnd, id);
        }
        _actions.Clear();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_actions.TryGetValue(id, out var callback))
            {
                callback?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.RemoveHook(HwndHook);
        _source.Dispose();
        GC.SuppressFinalize(this);
    }
}
