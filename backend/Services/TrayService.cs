using System;
using System.Collections.Generic;
using System.IO;
#if WINDOWS
using System.Drawing;
#endif
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
#if WINDOWS
    private bool _isConnected = false;
    private bool _isControlling = false;
#endif

#if !WINDOWS
    private readonly MacTrayManager? _macTray;
#endif

    // --- Native P/Invoke Declarations (Global scope for TrayService/MacTrayManager) ---
    [DllImport("libdl.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, string arg);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, double arg);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, ulong arg);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, int arg);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, byte arg);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, NSSize arg);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, int extraBytes);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern bool class_addMethod(IntPtr cls, IntPtr name, Delegate imp, string types);

    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern void objc_registerClassPair(IntPtr cls);

    [StructLayout(LayoutKind.Sequential)]
    private struct NSSize { public double width; public double height; }

#if WINDOWS
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private const int SW_HIDE    = 0;
    private const int SW_RESTORE = 9;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
#endif

    public TrayService(PhotinoWindow window, UniversalControlManager controlManager)
    {
        _window = window;
        _controlManager = controlManager;

#if WINDOWS
        LoadIcons();

        _notifyIcon = new NotifyIcon
        {
            Icon = _idleIcon ?? SystemIcons.Application,
            Text = "nicodemouse",
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

        var showItem       = new ToolStripMenuItem("Show / Restore", null, (s, e) => ShowWindow());
        var disconnectItem = new ToolStripMenuItem("Disconnect",     null, (s, e) => Disconnect());
        var exitItem       = new ToolStripMenuItem("Exit",           null, (s, e) => ExitApplication());

        contextMenu.Items.Add(showItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(disconnectItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowWindow();
#else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Oculta o ícone do Dock dinamicamente
            SetMacActivationPolicy(1); // 1 = NSApplicationActivationPolicyAccessory
            _macTray = new MacTrayManager(this);
        }
#endif

#if WINDOWS
        _window.WindowCreated += (sender, args) =>
        {
            var handle = _window.WindowHandle;
            if (handle != IntPtr.Zero)
            {
                int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
                exStyle |= WS_EX_TOOLWINDOW;
                exStyle &= ~WS_EX_APPWINDOW;
                SetWindowLong(handle, GWL_EXSTYLE, exStyle);
            }
        };
#endif

        _window.WindowClosing += (sender, args) =>
        {
            if (!_isExiting)
            {
                HideWindow();
                return true; // cancela o fechamento real
            }
            return false; // permite fechar de verdade
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC: ShowWindow
    // ─────────────────────────────────────────────────────────────
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
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _window.Invoke(() => {
                try
                {
                    IntPtr nsAppCls  = objc_getClass("NSApplication");
                    IntPtr sharedApp = objc_msgSend(nsAppCls, sel_registerName("sharedApplication"));

                    IntPtr nsWindow = GetMacWindowHandle();
                    if (nsWindow != IntPtr.Zero)
                    {
                        Console.WriteLine($"[MACTRAY] Showing window: {nsWindow}");
                        objc_msgSend(nsWindow, sel_registerName("makeKeyAndOrderFront:"), IntPtr.Zero);
                    }
                    else
                    {
                        Console.WriteLine("[MACTRAY] WARNING: Could not find window handle for ShowWindow");
                    }

                    objc_msgSend(sharedApp, sel_registerName("activateIgnoringOtherApps:"), (byte)1);
                    _window.SetMinimized(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MACTRAY] Error in ShowWindow: {ex}");
                }
            });
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC: HideWindow
    // ─────────────────────────────────────────────────────────────
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
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _window.Invoke(() => {
                try
                {
                    Console.WriteLine("[MACTRAY] HideWindow: Starting hiding sequence...");

                    IntPtr nsWindow = GetMacWindowHandle();
                    if (nsWindow != IntPtr.Zero)
                    {
                        Console.WriteLine($"[MACTRAY] HideWindow: Ordering window out: {nsWindow}");
                        objc_msgSend(nsWindow, sel_registerName("orderOut:"), IntPtr.Zero);
                    }
                    else
                    {
                        Console.WriteLine("[MACTRAY] HideWindow WARNING: Could not find window handle");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MACTRAY] Error in HideWindow: {ex}");
                }
            });
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE: Mac helpers
    // ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Define a NSApplicationActivationPolicy do app.
    /// 0 = Regular  (aparece no Dock)
    /// 1 = Accessory (NÃO aparece no Dock — só na status bar)
    /// 2 = Prohibited
    /// </summary>
    private void SetMacActivationPolicy(ulong policy)
    {
        try
        {
            IntPtr nsAppCls  = objc_getClass("NSApplication");
            IntPtr sharedApp = objc_msgSend(nsAppCls, sel_registerName("sharedApplication"));
            objc_msgSend(sharedApp, sel_registerName("setActivationPolicy:"), policy);
            Console.WriteLine($"[MACTRAY] Activation policy set to {policy}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MACTRAY] Error setting activation policy: {ex}");
        }
    }

    private IntPtr GetMacWindowHandle()
    {
        // Tentativa 1: Reflexão nos campos internos do PhotinoWindow
        try
        {
            string[] fieldNames = { "nativeWindow", "_nativeWindow" };
            foreach (var fieldName in fieldNames)
            {
                var field = typeof(PhotinoWindow).GetField(
                    fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null)
                {
                    var val = field.GetValue(_window);
                    if (val is IntPtr handle && handle != IntPtr.Zero)
                    {
                        Console.WriteLine($"[MACTRAY] Found handle via reflection '{fieldName}': {handle}");
                        return handle;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MACTRAY] Reflection failed for handle: {ex.Message}");
        }

        // Tentativa 2: Iterar janelas via Objective-C
        try
        {
            IntPtr nsAppCls  = objc_getClass("NSApplication");
            IntPtr sharedApp = objc_msgSend(nsAppCls, sel_registerName("sharedApplication"));
            IntPtr windows   = objc_msgSend(sharedApp, sel_registerName("windows"));
            ulong  count     = (ulong)objc_msgSend(windows, sel_registerName("count"));

            for (ulong i = 0; i < count; i++)
            {
                IntPtr win = objc_msgSend(windows, sel_registerName("objectAtIndex:"), i);
                if (win == IntPtr.Zero) continue;

                IntPtr titleNS = objc_msgSend(win, sel_registerName("title"));
                if (titleNS == IntPtr.Zero) continue;

                IntPtr utf8Ptr = objc_msgSend(titleNS, sel_registerName("UTF8String"));
                if (utf8Ptr == IntPtr.Zero) continue;

                string? title = Marshal.PtrToStringUTF8(utf8Ptr);
                if (title == "nicodemouse")
                {
                    Console.WriteLine($"[MACTRAY] Found handle via window iteration: {win}");
                    return win;
                }
            }
            Console.WriteLine("[MACTRAY] Window iteration finished without finding 'nicodemouse'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MACTRAY] Window iteration failed: {ex.Message}");
        }

        return IntPtr.Zero;
    }

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE: Disconnect / Exit
    // ─────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────
    //  WINDOWS: Icon management
    // ─────────────────────────────────────────────────────────────
#if WINDOWS
    private void LoadIcons()
    {
        try
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;

            string[] potentialAssetsPaths = {
                Path.Combine(exeDir, "Assets"),
                Path.Combine(exeDir, "..", "..", "..", "Assets"),
                Path.Combine(exeDir, "..", "..", "..", "..", "backend", "Assets"),
                Path.Combine(exeDir, "..", "..", "..", "..", "Assets"),
                exeDir
            };

            string? baseDir = null;
            foreach (var p in potentialAssetsPaths)
            {
                if (Directory.Exists(p) && File.Exists(Path.Combine(p, "tray_idle.ico")))
                {
                    baseDir = p;
                    break;
                }
            }

            if (baseDir == null) baseDir = exeDir;
            Console.WriteLine($"[TRAY] Final Icon Path: {baseDir}");

            LoadIconSet(baseDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRAY] Error in LoadIcons: {ex.Message}");
            _idleIcon      ??= SystemIcons.Application;
            _connectedIcon ??= _idleIcon;
        }
    }

    private void LoadIconSet(string baseDir)
    {
        string idlePath      = Path.Combine(baseDir, "tray_idle.ico");
        string connectedPath = Path.Combine(baseDir, "tray_connected.ico");

        try
        {
            if (File.Exists(idlePath))      _idleIcon      = new Icon(idlePath);
            if (File.Exists(connectedPath)) _connectedIcon = new Icon(connectedPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRAY] Critical: Failed to load idle/connected icons: {ex.Message}");
        }

        _idleIcon      ??= SystemIcons.Application;
        _connectedIcon ??= _idleIcon;

        _activeFrames.Clear();
        for (int i = 1; i <= 6; i++)
        {
            string framePath = Path.Combine(baseDir, $"tray_active_{i}.ico");
            try
            {
                if (File.Exists(framePath)) _activeFrames.Add(new Icon(framePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TRAY] Error loading active frame {i}: {ex.Message}");
            }
        }
    }

    private void UpdateTrayIcon()
    {
        if (_notifyIcon == null) return;

        if (_isControlling && _activeFrames.Count > 0)
        {
            _animationTimer?.Start();
        }
        else
        {
            _animationTimer?.Stop();
            _notifyIcon.Icon = _isControlling
                ? (_activeFrames.Count > 0 ? _activeFrames[0] : _connectedIcon)
                : (_isConnected ? _connectedIcon : _idleIcon);
        }
    }

    private void CycleActiveIcon()
    {
        if (_notifyIcon == null || _activeFrames.Count == 0) return;
        _currentFrame    = (_currentFrame + 1) % _activeFrames.Count;
        _notifyIcon.Icon = _activeFrames[_currentFrame];
    }
#endif

    // ─────────────────────────────────────────────────────────────
    //  IDisposable
    // ─────────────────────────────────────────────────────────────
    public void Dispose()
    {
#if WINDOWS
        _animationTimer?.Stop();
        _animationTimer?.Dispose();
        _notifyIcon?.Dispose();
#else
        _macTray?.Dispose();
#endif
    }

    // ─────────────────────────────────────────────────────────────
    //  MAC: MacTrayManager (status bar nativa via Obj-C)
    // ─────────────────────────────────────────────────────────────
#if !WINDOWS
    private class MacTrayManager : IDisposable
    {
        private readonly TrayService _parent;
        private IntPtr _statusItem;
        private IntPtr _statusBar;
        private IntPtr _menu;
        private IntPtr _target;

        private delegate void ActionDelegate(IntPtr self, IntPtr _cmd, IntPtr sender);
        private readonly ActionDelegate _onShow;
        private readonly ActionDelegate _onExit;
        private readonly ActionDelegate _onDisconnect;

        public MacTrayManager(TrayService parent)
        {
            _parent = parent;

            _onShow = (self, cmd, sender) => {
                try { parent.ShowWindow(); }
                catch (Exception ex) { Console.WriteLine($"[MACTRAY] Callback Error (Show): {ex}"); }
            };
            _onExit = (self, cmd, sender) => {
                try { parent.ExitApplication(); }
                catch (Exception ex) { Console.WriteLine($"[MACTRAY] Callback Error (Exit): {ex}"); }
            };
            _onDisconnect = (self, cmd, sender) => {
                try { parent.Disconnect(); }
                catch (Exception ex) { Console.WriteLine($"[MACTRAY] Callback Error (Disconnect): {ex}"); }
            };

            try { InitializeTray(); }
            catch (Exception ex) { Console.WriteLine($"[MACTRAY] Critical Exception during initialization: {ex}"); }
        }

        private void InitializeTray()
        {
            Console.WriteLine("[MACTRAY] Starting initialization...");

            // 0. Garante que AppKit está carregado
            IntPtr appKit = dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 2);
            Console.WriteLine($"[MACTRAY] AppKit library handle: {appKit}");

            // 1. Cria instância alvo para receber ações de menu
            _target = CreateTargetInstance();
            Console.WriteLine($"[MACTRAY] Target instance created: {_target}");

            // 2. NSStatusBar.systemStatusBar
            IntPtr nsStatusBarCls = objc_getClass("NSStatusBar");
            Console.WriteLine($"[MACTRAY] NSStatusBar class: {nsStatusBarCls}");
            if (nsStatusBarCls == IntPtr.Zero) return;

            _statusBar = objc_msgSend(nsStatusBarCls, sel_registerName("systemStatusBar"));
            Console.WriteLine($"[MACTRAY] System status bar handle: {_statusBar}");
            if (_statusBar == IntPtr.Zero) return;

            // 3. statusItemWithLength: -1 (NSSquareStatusItemLength)
            _statusItem = objc_msgSend(_statusBar, sel_registerName("statusItemWithLength:"), (double)-1);
            Console.WriteLine($"[MACTRAY] Status item handle: {_statusItem}");
            if (_statusItem == IntPtr.Zero) return;

            objc_msgSend(_statusItem, sel_registerName("setHighlightMode:"), 1);

            // 4. Ícone
            string iconPath = FindIcon();
            if (!string.IsNullOrEmpty(iconPath))
            {
                Console.WriteLine($"[MACTRAY] Loading icon from: {iconPath}");
                IntPtr nsStringPath = objc_msgSend(
                    objc_getClass("NSString"),
                    sel_registerName("stringWithUTF8String:"),
                    iconPath);

                IntPtr image = objc_msgSend(objc_getClass("NSImage"), sel_registerName("alloc"));
                objc_msgSend(image, sel_registerName("initWithContentsOfFile:"), nsStringPath);
                objc_msgSend(image, sel_registerName("setSize:"), new NSSize { width = 18, height = 18 });
                objc_msgSend(image, sel_registerName("setTemplate:"), 1); // adapta ao dark/light mode

                IntPtr button = objc_msgSend(_statusItem, sel_registerName("button"));
                Console.WriteLine($"[MACTRAY] Status item button handle: {button}");
                objc_msgSend(button, sel_registerName("setImage:"), image);
            }
            else
            {
                Console.WriteLine("[MACTRAY] WARNING: Icon file not found!");
            }

            // 5. Menu
            _menu = objc_msgSend(objc_getClass("NSMenu"), sel_registerName("alloc"));
            objc_msgSend(_menu, sel_registerName("initWithTitle:"),
                objc_msgSend(objc_getClass("NSString"),
                    sel_registerName("stringWithUTF8String:"), "nicodemouse"));

            AddMenuItem("Show / Restore", sel_registerName("onShow:"));
            AddMenuSeparator();
            AddMenuItem("Disconnect",     sel_registerName("onDisconnect:"));
            AddMenuSeparator();
            AddMenuItem("Exit",           sel_registerName("onExit:"));

            objc_msgSend(_statusItem, sel_registerName("setMenu:"), _menu);
            Console.WriteLine("[MACTRAY] Tray initialized successfully.");
        }

        private IntPtr CreateTargetInstance()
        {
            IntPtr cls = objc_allocateClassPair(objc_getClass("NSObject"), "TrayTarget", 0);
            class_addMethod(cls, sel_registerName("onShow:"),       _onShow,       "v@:@");
            class_addMethod(cls, sel_registerName("onExit:"),       _onExit,       "v@:@");
            class_addMethod(cls, sel_registerName("onDisconnect:"), _onDisconnect, "v@:@");
            objc_registerClassPair(cls);
            return objc_msgSend(
                objc_msgSend(cls, sel_registerName("alloc")),
                sel_registerName("init"));
        }

        private string FindIcon()
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] paths = {
                    Path.GetFullPath(Path.Combine(exeDir, "logo_n.png")),
                    Path.GetFullPath(Path.Combine(exeDir, "Assets", "logo_n.png")),
                    Path.GetFullPath(Path.Combine(exeDir, "backend", "Assets", "logo_n.png")),
                    Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "Assets", "logo_n.png")),
                    Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "backend", "Assets", "logo_n.png"))
                };

                foreach (var p in paths)
                {
                    if (File.Exists(p))
                    {
                        Console.WriteLine($"[MACTRAY] Found icon at: {p}");
                        return p;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MACTRAY] Error finding icon: {ex.Message}");
            }
            return "";
        }

        private void AddMenuItem(string title, IntPtr selector)
        {
            IntPtr nsTitle = objc_msgSend(
                objc_getClass("NSString"),
                sel_registerName("stringWithUTF8String:"), title);

            IntPtr item = objc_msgSend(objc_getClass("NSMenuItem"), sel_registerName("alloc"));

            objc_msgSend(item, sel_registerName("initWithTitle:action:keyEquivalent:"),
                nsTitle, selector,
                objc_msgSend(objc_getClass("NSString"),
                    sel_registerName("stringWithUTF8String:"), ""));

            objc_msgSend(item, sel_registerName("setTarget:"), _target);
            objc_msgSend(_menu, sel_registerName("addItem:"), item);
        }

        private void AddMenuSeparator()
        {
            IntPtr sep = objc_msgSend(objc_getClass("NSMenuItem"), sel_registerName("separatorItem"));
            objc_msgSend(_menu, sel_registerName("addItem:"), sep);
        }

        public void Dispose()
        {
            if (_statusItem != IntPtr.Zero)
            {
                objc_msgSend(_statusBar, sel_registerName("removeStatusItem:"), _statusItem);
                _statusItem = IntPtr.Zero;
            }
        }
    }
#endif
}
