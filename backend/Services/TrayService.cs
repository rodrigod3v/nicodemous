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
    private const string ObjCLib = "/usr/lib/libobjc.A.dylib";

    [DllImport("libdl.dylib")]
    private static extern IntPtr dlopen(string path, int mode);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLib)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, string arg);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, double arg);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, ulong arg);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, int arg);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, byte arg);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    // REMOVED: [DllImport(ObjCLib)] private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, NSSize arg);
    // Passing structs by value in objc_msgSend is highly ABI-dependent (ARM64 vs x64).
    // We'll avoid it by not calling setSize: directly or using a different approach.

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, int extraBytes);

    [DllImport(ObjCLib)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr name, Delegate imp, string types);

    [DllImport(ObjCLib)]
    private static extern void objc_registerClassPair(IntPtr cls);

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

                    Console.WriteLine("[MACTRAY] Unhiding application...");
                    SetMacActivationPolicy(0); // 0 = NSApplicationActivationPolicyRegular
                    objc_msgSend(sharedApp, sel_registerName("unhide:"), IntPtr.Zero);
                    objc_msgSend(sharedApp, sel_registerName("activateIgnoringOtherApps:"), (byte)1);

                    IntPtr nsWindow = GetMacWindowHandle();
                    if (nsWindow != IntPtr.Zero)
                    {
                        objc_msgSend(nsWindow, sel_registerName("makeKeyAndOrderFront:"), IntPtr.Zero);
                    }
                    
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
            try
            {
                Console.WriteLine("[MACTRAY] HideWindow: Minimizing window and hiding from Dock...");
                _window.Invoke(() => _window.SetMinimized(true));
                SetMacActivationPolicy(1); // 1 = NSApplicationActivationPolicyAccessory
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MACTRAY] Error in HideWindow: {ex}");
            }
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
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            
            _idleIcon = LoadEmbeddedIcon(assembly, "tray_idle.ico");
            _connectedIcon = LoadEmbeddedIcon(assembly, "tray_connected.ico") ?? _idleIcon;

            _idleIcon ??= SystemIcons.Application;
            _connectedIcon ??= _idleIcon;

            _activeFrames.Clear();
            for (int i = 1; i <= 6; i++)
            {
                var frame = LoadEmbeddedIcon(assembly, $"tray_active_{i}.ico");
                if (frame != null)
                {
                    _activeFrames.Add(frame);
                }
            }

            Console.WriteLine($"[TRAY] Embedded Icons Loaded. Frames: {_activeFrames.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRAY] Error in LoadIcons: {ex.Message}");
            _idleIcon      ??= SystemIcons.Application;
            _connectedIcon ??= _idleIcon;
        }
    }

    private Icon? LoadEmbeddedIcon(System.Reflection.Assembly assembly, string filename)
    {
        try
        {
            using var stream = assembly.GetManifestResourceStream($"nicodemouse_backend.Assets.{filename}");
            if (stream != null)
            {
                return new Icon(stream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TRAY] Failed to load embedded {filename}: {ex.Message}");
        }
        return null;
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

            // 0. Ensure AppKit is loaded
            IntPtr appKit = dlopen("/System/Library/Frameworks/AppKit.framework/AppKit", 2);
            if (appKit == IntPtr.Zero)
            {
                Console.WriteLine("[MACTRAY] CRITICAL: Failed to load AppKit framework via dlopen.");
                return;
            }
            Console.WriteLine($"[MACTRAY] AppKit library handle: {appKit}");

            // 1. Create target instance for menu actions
            _target = CreateTargetInstance();
            if (_target == IntPtr.Zero)
            {
                Console.WriteLine("[MACTRAY] CRITICAL: Failed to create Objective-C target instance.");
                return;
            }
            Console.WriteLine($"[MACTRAY] Target instance created: {_target}");

            // 2. NSStatusBar.systemStatusBar
            IntPtr nsStatusBarCls = objc_getClass("NSStatusBar");
            if (nsStatusBarCls == IntPtr.Zero)
            {
                Console.WriteLine("[MACTRAY] CRITICAL: NSStatusBar class not found.");
                return;
            }

            _statusBar = objc_msgSend(nsStatusBarCls, sel_registerName("systemStatusBar"));
            if (_statusBar == IntPtr.Zero)
            {
                Console.WriteLine("[MACTRAY] CRITICAL: systemStatusBar returned NULL.");
                return;
            }
            Console.WriteLine($"[MACTRAY] System status bar handle: {_statusBar}");

            // 3. statusItemWithLength: -1 (NSSquareStatusItemLength)
            _statusItem = objc_msgSend(_statusBar, sel_registerName("statusItemWithLength:"), (double)-1);
            if (_statusItem == IntPtr.Zero)
            {
                Console.WriteLine("[MACTRAY] CRITICAL: Failed to create status item (statusItemWithLength returned NULL).");
                return;
            }
            Console.WriteLine($"[MACTRAY] Status item handle: {_statusItem}");

            objc_msgSend(_statusItem, sel_registerName("setHighlightMode:"), 1);

            // 4. Icon
            string iconPath = FindIcon();
            if (!string.IsNullOrEmpty(iconPath))
            {
                Console.WriteLine($"[MACTRAY] Loading icon from: {iconPath}");
                IntPtr nsStringCls = objc_getClass("NSString");
                if (nsStringCls != IntPtr.Zero)
                {
                    IntPtr nsStringPath = objc_msgSend(
                        nsStringCls,
                        sel_registerName("stringWithUTF8String:"),
                        iconPath);

                    IntPtr nsImageCls = objc_getClass("NSImage");
                    if (nsImageCls != IntPtr.Zero)
                    {
                        IntPtr image = objc_msgSend(nsImageCls, sel_registerName("alloc"));
                        if (image != IntPtr.Zero)
                        {
                            objc_msgSend(image, sel_registerName("initWithContentsOfFile:"), nsStringPath);
                            objc_msgSend(image, sel_registerName("setTemplate:"), 1); // adapts to dark/light mode

                            IntPtr button = objc_msgSend(_statusItem, sel_registerName("button"));
                            if (button != IntPtr.Zero)
                            {
                                Console.WriteLine($"[MACTRAY] Applying icon to status item button: {button}");
                                objc_msgSend(button, sel_registerName("setImage:"), image);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("[MACTRAY] WARNING: Icon file not found, tray item may be invisible.");
            }

            // 5. Menu
            try 
            {
                IntPtr nsMenuCls = objc_getClass("NSMenu");
                if (nsMenuCls == IntPtr.Zero)
                {
                    Console.WriteLine("[MACTRAY] CRITICAL: NSMenu class not found.");
                    return;
                }

                _menu = objc_msgSend(nsMenuCls, sel_registerName("alloc"));
                if (_menu == IntPtr.Zero)
                {
                    Console.WriteLine("[MACTRAY] CRITICAL: Failed to allocate NSMenu.");
                    return;
                }

                IntPtr nsStringCls = objc_getClass("NSString");
                objc_msgSend(_menu, sel_registerName("initWithTitle:"),
                    objc_msgSend(nsStringCls,
                        sel_registerName("stringWithUTF8String:"), "nicodemouse"));

                AddMenuItem("Show / Restore", sel_registerName("onShow:"));
                AddMenuSeparator();
                AddMenuItem("Disconnect",     sel_registerName("onDisconnect:"));
                AddMenuSeparator();
                AddMenuItem("Exit",           sel_registerName("onExit:"));

                objc_msgSend(_statusItem, sel_registerName("setMenu:"), _menu);
                Console.WriteLine("[MACTRAY] Tray initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MACTRAY] Error creating menu: {ex}");
            }
        }

        private IntPtr CreateTargetInstance()
        {
            const string className = "TrayTarget";
            IntPtr cls = objc_getClass(className);
            
            if (cls == IntPtr.Zero)
            {
                IntPtr nsObject = objc_getClass("NSObject");
                if (nsObject == IntPtr.Zero)
                {
                    Console.WriteLine("[MACTRAY] CRITICAL: NSObject class not found.");
                    return IntPtr.Zero;
                }

                cls = objc_allocateClassPair(nsObject, className, 0);
                if (cls == IntPtr.Zero)
                {
                    Console.WriteLine("[MACTRAY] WARNING: Failed to allocate class pair for TrayTarget (maybe already exists).");
                    // Try to get it again just in case a race condition occurred
                    cls = objc_getClass(className);
                    if (cls == IntPtr.Zero) return IntPtr.Zero;
                }
                else
                {
                    class_addMethod(cls, sel_registerName("onShow:"),       _onShow,       "v@:@");
                    class_addMethod(cls, sel_registerName("onExit:"),       _onExit,       "v@:@");
                    class_addMethod(cls, sel_registerName("onDisconnect:"), _onDisconnect, "v@:@");
                    objc_registerClassPair(cls);
                    Console.WriteLine("[MACTRAY] TrayTarget class successfully registered.");
                }
            }

            IntPtr alloc = objc_msgSend(cls, sel_registerName("alloc"));
            if (alloc == IntPtr.Zero) return IntPtr.Zero;
            
            return objc_msgSend(alloc, sel_registerName("init"));
        }

        private string FindIcon()
        {
            try
            {
                var iconStream = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("nicodemouse_backend.Assets.logo_n.png");

                if (iconStream != null)
                {
                    var tempIconPath = Path.Combine(Path.GetTempPath(), "nicodemouse_temp_logo_n.png");
                    using (var fileStream = File.Create(tempIconPath))
                    {
                        iconStream.CopyTo(fileStream);
                    }
                    Console.WriteLine($"[MACTRAY] Embedded icon extracted to: {tempIconPath}");
                    return tempIconPath;
                }
                
                Console.WriteLine("[MACTRAY] WARNING: Embedded logo_n.png not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MACTRAY] Error extracting embedded icon: {ex.Message}");
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
