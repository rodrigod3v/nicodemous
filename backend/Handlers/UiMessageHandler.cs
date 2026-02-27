using System.Text.Json;
using Photino.NET;
using System.Runtime.InteropServices;
using nicodemouse.Backend.Models;
using nicodemouse.Backend.Services;

namespace nicodemouse.Backend.Handlers;

public class UiMessageHandler
{
    private readonly UniversalControlManager _controlManager;
    private readonly PhotinoWindow _window;

#if WINDOWS
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int HT_CAPTION = 0x2;
#else
    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_getClass(string name);
    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr sel_registerName(string name);
    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);
    [DllImport("/usr/lib/libobjc.A.dylib")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, ulong arg);
#endif

    public UiMessageHandler(UniversalControlManager controlManager, PhotinoWindow window)
    {
        _controlManager = controlManager;
        _window = window;
    }

    public async Task HandleMessageAsync(string message)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var baseMessage = JsonSerializer.Deserialize<UiMessage>(message, options);

            if (baseMessage == null) return;

            Console.WriteLine($"[BACKEND] Handling UI Message: {baseMessage.Type}");

            switch (baseMessage.Type)
            {
                case "start_discovery":
                    _controlManager.RefreshDiscovery();
                    var devices = _controlManager.GetDevices();
                    _window.SendWebMessage(JsonSerializer.Serialize(new { 
                        type = "discovery_result", 
                        devices 
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                    break;

                case "service_toggle":
                    var toggleMsg = JsonSerializer.Deserialize<ServiceToggleMessage>(message, options);
                    if (toggleMsg != null)
                        _controlManager.ToggleService(toggleMsg.Service, toggleMsg.Enabled);
                    break;

                case "connect_device":
                    var connectMsg = JsonSerializer.Deserialize<ConnectDeviceMessage>(message, options);
                    if (connectMsg != null)
                    {
                        string ipOrCode = connectMsg.Ip ?? connectMsg.Code ?? "";
                        if (!string.IsNullOrEmpty(ipOrCode))
                            await _controlManager.ConnectAsync(ipOrCode, _window);
                    }
                    break;

                case "get_settings":
                    _controlManager.SendSettingsToWeb();
                    break;

                case "update_settings":
                    var updateMsg = JsonSerializer.Deserialize<UpdateSettingsMessage>(message, options);
                    if (updateMsg != null)
                    {
                        _controlManager.UpdateSettings(
                            updateMsg.Edge,
                            updateMsg.LockInput,
                            updateMsg.Delay,
                            updateMsg.CornerSize,
                            updateMsg.Sensitivity,
                            updateMsg.GestureThreshold,
                            updateMsg.PairingCode,
                            updateMsg.ActiveMonitor
                        );
                    }
                    break;

                case "reset_settings":
                    _controlManager.ResetSettings();
                    break;
                    
                case "exit_app":
                    Environment.Exit(0);
                    break;
                    
                case "close_app":
                    _window.Close();
                    break;
                    
                case "hide_app":
                case "minimize_app":
#if !WINDOWS
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        try
                        {
                            IntPtr nsAppCls = objc_getClass("NSApplication");
                            IntPtr sharedApp = objc_msgSend(nsAppCls, sel_registerName("sharedApplication"));
                            _window.SetMinimized(true);
                            objc_msgSend(sharedApp, sel_registerName("setActivationPolicy:"), (ulong)1); // Hide from Dock
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[MACTRAY] Error hiding app: {ex}");
                        }
                    }
                    else
                    {
                        _window.SetMinimized(true);
                    }
#else
                    _window.SetMinimized(true);
#endif
                    break;
                    
                case "move_app":
#if !WINDOWS
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        using (var doc = JsonDocument.Parse(message))
                        {
                            if (doc.RootElement.TryGetProperty("dx", out var dxEl) && doc.RootElement.TryGetProperty("dy", out var dyEl))
                            {
                                int dx = dxEl.GetInt32();
                                int dy = dyEl.GetInt32();
                                var loc = _window.Location;
                                _window.SetLocation(new System.Drawing.Point(loc.X + dx, loc.Y + dy));
                            }
                        }
                    }
#endif
                    break;
                    
                case "drag_app":
#if WINDOWS
                    ReleaseCapture();
                    SendMessage(_window.WindowHandle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
#endif
                    break;
                    
                default:
                    Console.WriteLine($"[BACKEND] Unknown message type: {baseMessage.Type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UI HANDLER ERROR] {ex.Message}");
        }
    }

#if !WINDOWS
    private IntPtr GetMacWindowHandle()
    {
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
                        return handle;
                    }
                }
            }

            // Fallback: iterate windows
            IntPtr nsAppCls = objc_getClass("NSApplication");
            IntPtr sharedApp = objc_msgSend(nsAppCls, sel_registerName("sharedApplication"));
            IntPtr windows = objc_msgSend(sharedApp, sel_registerName("windows"));
            ulong count = (ulong)objc_msgSend(windows, sel_registerName("count"));

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
                    return win;
                }
            }
        }
        catch (Exception) { }
        return IntPtr.Zero;
    }
#endif
}
