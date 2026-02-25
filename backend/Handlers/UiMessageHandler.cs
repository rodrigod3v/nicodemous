using System.Text.Json;
using Photino.NET;
using nicodemouse.Backend.Models;
using nicodemouse.Backend.Services;

namespace nicodemouse.Backend.Handlers;

public class UiMessageHandler
{
    private readonly UniversalControlManager _controlManager;
    private readonly PhotinoWindow _window;

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
