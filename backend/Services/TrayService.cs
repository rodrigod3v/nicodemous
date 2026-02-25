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
#if !WINDOWS
    private readonly MacTrayManager? _macTray;
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
#else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _macTray = new MacTrayManager(this);
        }
#endif

        // Handle window close event to minimize to tray
        _window.WindowClosing += (sender, args) =>
        {
            if (!_isExiting)
            {
                HideWindow();
                return true; 
            }
            return false; 
        };
    }

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
                _window.SetMinimized(false);
            });
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
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _window.Invoke(() => {
                _window.SetMinimized(true);
            });
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

#if WINDOWS
    // ... (Keep existing Windows Icon methods) ...
#endif

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
            _onShow = (self, cmd, sender) => _parent.ShowWindow();
            _onExit = (self, cmd, sender) => _parent.ExitApplication();
            _onDisconnect = (self, cmd, sender) => _parent.Disconnect();

            try {
                InitializeTray();
            } catch (Exception ex) {
                Console.WriteLine($"[MACTRAY] Error initializing: {ex.Message}");
            }
        }

        private void InitializeTray()
        {
            // 1. Register a target class to receive actions
            _target = CreateTargetInstance();

            // 2. Get NSStatusBar.systemStatusBar
            _statusBar = objc_msgSend(objc_getClass("NSStatusBar"), sel_registerName("systemStatusBar"));
            
            // 3. statusItemWithLength: -1 (NSSquareStatusItemLength)
            _statusItem = objc_msgSend(_statusBar, sel_registerName("statusItemWithLength:"), (double)-1);
            objc_msgSend(_statusItem, sel_registerName("setHighlightMode:"), 1);

            // 4. Set Icon Image
            string iconPath = FindIcon();
            if (!string.IsNullOrEmpty(iconPath))
            {
                IntPtr nsStringPath = objc_msgSend(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), iconPath);
                IntPtr image = objc_msgSend(objc_getClass("NSImage"), sel_registerName("alloc"));
                objc_msgSend(image, sel_registerName("initWithContentsOfFile:"), nsStringPath);
                
                objc_msgSend(image, sel_registerName("setSize:"), new NSSize { width = 18, height = 18 });
                objc_msgSend(image, sel_registerName("setTemplate:"), 1); 

                IntPtr button = objc_msgSend(_statusItem, sel_registerName("button"));
                objc_msgSend(button, sel_registerName("setImage:"), image);
            }

            // 5. Create Menu
            _menu = objc_msgSend(objc_getClass("NSMenu"), sel_registerName("alloc"));
            objc_msgSend(_menu, sel_registerName("initWithTitle:"), 
                objc_msgSend(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), "nicodemouse"));

            AddMenuItem("Show / Restore", sel_registerName("onShow:"));
            AddMenuSeparator();
            AddMenuItem("Disconnect", sel_registerName("onDisconnect:"));
            AddMenuSeparator();
            AddMenuItem("Exit", sel_registerName("onExit:"));

            objc_msgSend(_statusItem, sel_registerName("setMenu:"), _menu);
        }

        private IntPtr CreateTargetInstance()
        {
            IntPtr cls = objc_allocateClassPair(objc_getClass("NSObject"), "TrayTarget", 0);
            class_addMethod(cls, sel_registerName("onShow:"), _onShow, "v@:@");
            class_addMethod(cls, sel_registerName("onExit:"), _onExit, "v@:@");
            class_addMethod(cls, sel_registerName("onDisconnect:"), _onDisconnect, "v@:@");
            objc_registerClassPair(cls);
            return objc_msgSend(objc_msgSend(cls, sel_registerName("alloc")), sel_registerName("init"));
        }

        private string FindIcon()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths = {
                Path.GetFullPath(Path.Combine(exeDir, "logo_n.png")),
                Path.GetFullPath(Path.Combine(exeDir, "Assets", "logo_n.png")),
                Path.GetFullPath(Path.Combine(exeDir, "backend", "Assets", "logo_n.png"))
            };
            foreach (var p in paths) if (File.Exists(p)) return p;
            return "";
        }

        private void AddMenuItem(string title, IntPtr selector)
        {
            IntPtr nsTitle = objc_msgSend(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), title);
            IntPtr item = objc_msgSend(objc_getClass("NSMenuItem"), sel_registerName("alloc"));
            
            objc_msgSend(item, sel_registerName("initWithTitle:action:keyEquivalent:"), 
                nsTitle, selector, 
                objc_msgSend(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), ""));

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
    }
#endif
#endif
}
