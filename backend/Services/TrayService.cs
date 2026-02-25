using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
#if WINDOWS
using System.Windows.Forms;
#endif
using Photino.NET;
using nicodemouse.Backend.Services;

namespace nicodemouse.Backend.Services;

public class TrayService : IDisposable
{
    private readonly PhotinoWindow _window;
    private readonly UniversalControlManager _controlManager;
#if WINDOWS
    private readonly NotifyIcon? _notifyIcon;
#endif
    private bool _isExiting = false;

    public TrayService(PhotinoWindow window, UniversalControlManager controlManager)
    {
        _window = window;
        _controlManager = controlManager;

#if WINDOWS
        string iconPath = Path.GetFullPath("../frontend/public/favicon.ico");
        _notifyIcon = new NotifyIcon
        {
            Icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application,
            Text = "nicodemouse - Universal Control",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();
        
        var showItem = new ToolStripMenuItem("Show / Restore", null, (s, e) => ShowWindow());
        var disconnectItem = new ToolStripMenuItem("Disconnect", null, (s, e) => Disconnect());
        var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => ExitApplication());

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(disconnectItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowWindow();
#endif

        // Handle window close event to minimize to tray
        _window.WindowClosing += (sender, args) =>
        {
            if (!_isExiting)
            {
                HideWindow();
                return true; // Cancel close, just hide
            }
            return false; // Allow close if exiting from tray
        };
    }

#if WINDOWS
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
#endif

    public void ShowWindow()
    {
        Console.WriteLine("[TRAY] ShowWindow called.");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
#if WINDOWS
            _window.Invoke(() => {
                Console.WriteLine($"[TRAY] Restoring window handle: {_window.WindowHandle}");
                _window.SetMinimized(false);
                ShowWindow(_window.WindowHandle, SW_RESTORE);
                SetForegroundWindow(_window.WindowHandle);
            });
#endif
        }
    }

    public void HideWindow()
    {
        Console.WriteLine("[TRAY] HideWindow called.");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
#if WINDOWS
            _window.Invoke(() => {
                Console.WriteLine($"[TRAY] Hiding window handle: {_window.WindowHandle}");
                ShowWindow(_window.WindowHandle, SW_HIDE);
            });
#endif
        }
    }

    private void Disconnect()
    {
        _controlManager.ToggleService("disconnect", true);
#if WINDOWS
        _notifyIcon?.ShowBalloonTip(2000, "nicodemouse", "Disconnected from remote session.", ToolTipIcon.Info);
#endif
    }

    private void ExitApplication()
    {
        _isExiting = true;
#if WINDOWS
        if (_notifyIcon != null) _notifyIcon.Visible = false;
#endif
        _window.Close();
    }

    public void Dispose()
    {
#if WINDOWS
        _notifyIcon?.Dispose();
#endif
    }
}
