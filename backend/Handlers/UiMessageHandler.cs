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
                    
                case "minimize_app":
                    _window.SetMinimized(true);
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
}
