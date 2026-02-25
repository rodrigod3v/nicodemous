using System;
using System.Collections.Generic;
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

#if WINDOWS
    private readonly List<Icon> _activeFrames = new();
    private Icon? _idleIcon;
    private Icon? _connectedIcon;
    private int _currentFrame = 0;
#endif
#if WINDOWS
    private System.Windows.Forms.Timer? _animationTimer;
#endif
    private bool _isConnected = false;
    private bool _isControlling = false;

    public TrayService(PhotinoWindow window, UniversalControlManager controlManager)
    {
        _window = window;
        _controlManager = controlManager;

#if WINDOWS
        LoadIcons();

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon ?? SystemIcons.Application,
            Text = "Nicodemous",
            Visible = true
        };

        _animationTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _animationTimer.Tick += (s, e) => CycleActiveIcon();

        _controlManager.OnConnectionChanged += (connected) => {
            _isConnected = connected;
            UpdateTrayIcon();
        };

        _controlManager.OnRemoteControlChanged += (active) => {
            _isControlling = active;
            UpdateTrayIcon();
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
#if WINDOWS
                HideWindow();
                return true; // Cancel close, just hide
#else
                return false; // Allow close on other platforms (for now)
#endif
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
#if WINDOWS
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
#endif
    }

    public void HideWindow()
    {
        Console.WriteLine("[TRAY] HideWindow called.");
#if WINDOWS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
#if WINDOWS
            _window.Invoke(() => {
                Console.WriteLine($"[TRAY] Hiding window handle: {_window.WindowHandle}");
                ShowWindow(_window.WindowHandle, SW_HIDE);
            });
#endif
        }
#endif
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

#if WINDOWS
    private void LoadIcons()
    {
        try {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // 1. Look for Assets folder locally or in project structure
            string[] potentialAssetsPaths = {
                Path.Combine(exeDir, "Assets"),                                   // Deployed alongside exe
                Path.Combine(exeDir, "..", "..", "..", "Assets"),                  // Near bin/Debug
                Path.Combine(exeDir, "..", "..", "..", "..", "backend", "Assets"), // Project root dev
                Path.Combine(exeDir, "..", "..", "..", "..", "Assets"),           // Project root generic
                exeDir                                                            // Fallback to exe dir
            };

            string? baseDir = null;
            foreach (var p in potentialAssetsPaths) {
                if (Directory.Exists(p) && File.Exists(Path.Combine(p, "tray_idle.ico"))) {
                    baseDir = p;
                    break;
                }
            }

            if (baseDir == null) baseDir = exeDir;
            Console.WriteLine($"[TRAY] Final Icon Path: {baseDir}");

            LoadIconSet(baseDir);

        } catch (Exception ex) {
            Console.WriteLine($"[TRAY] Error in LoadIcons: {ex.Message}");
            _idleIcon ??= SystemIcons.Application;
            _connectedIcon ??= _idleIcon;
        }
    }

    private void LoadIconSet(string baseDir)
    {
        string idlePath = Path.Combine(baseDir, "tray_idle.ico");
        string connectedPath = Path.Combine(baseDir, "tray_connected.ico");
        
        try {
            if (File.Exists(idlePath)) _idleIcon = new Icon(idlePath);
            if (File.Exists(connectedPath)) _connectedIcon = new Icon(connectedPath);
        } catch (Exception ex) {
            Console.WriteLine($"[TRAY] Critical: Failed to load idle/connected icons: {ex.Message}");
        }

        _idleIcon ??= SystemIcons.Application;
        _connectedIcon ??= _idleIcon;

        _activeFrames.Clear();
        for (int i = 1; i <= 6; i++) {
            string framePath = Path.Combine(baseDir, $"tray_active_{i}.ico");
            try {
                if (File.Exists(framePath)) _activeFrames.Add(new Icon(framePath));
            } catch (Exception ex) {
                Console.WriteLine($"[TRAY] Error loading active frame {i}: {ex.Message}");
            }
        }
    }
#endif

    private void UpdateTrayIcon()
    {
#if WINDOWS
        if (_notifyIcon == null) return;

        if (_isControlling && _activeFrames.Count > 0) {
            _animationTimer?.Start();
        } else {
            _animationTimer?.Stop();
            _notifyIcon.Icon = _isControlling ? (_activeFrames.Count > 0 ? _activeFrames[0] : _connectedIcon) 
                             : (_isConnected ? _connectedIcon : _idleIcon);
        }
#endif
    }

    private void CycleActiveIcon()
    {
#if WINDOWS
        if (_notifyIcon == null || _activeFrames.Count == 0) return;
        _currentFrame = (_currentFrame + 1) % _activeFrames.Count;
        _notifyIcon.Icon = _activeFrames[_currentFrame];
#endif
    }

    public void Dispose()
    {
#if WINDOWS
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _notifyIcon?.Dispose();
#endif
    }
}
