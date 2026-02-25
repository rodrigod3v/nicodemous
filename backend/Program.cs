using System;
using System.IO;
using System.Text.Json;
using Photino.NET;
using nicodemouse.Backend.Services;

namespace nicodemouse.Backend;

class Program
{
    private static UniversalControlManager? _controlManager;

    [STAThread]
    static void Main(string[] args)
    {
        string windowTitle = "nicodemouse - Universal Control";
        
#if DEBUG
        string initialUrl = "http://localhost:5173"; 
#else
        string initialUrl = "wwwroot/index.html";
#endif

        var window = new PhotinoWindow()
            .SetTitle(windowTitle)
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 850)
            .Center()
            .SetResizable(true)
            .SetIconFile(Path.GetFullPath("../frontend/public/favicon.ico"));

        // Initialize Central Manager
        var settings = new SettingsService();
        _controlManager = new UniversalControlManager(settings);
        _controlManager.SetWindow(window);

        // Initialize Tray Support
        using var trayService = new TrayService(window, _controlManager);


        // UI Callbacks
        window.RegisterWebMessageReceivedHandler((object? sender, string message) => 
        {
            var windowRef = (PhotinoWindow)sender!;
            ProcessUiMessage(message, windowRef);
        });

        _controlManager.Start();
        
        window.Load(initialUrl);

        // Send actual Pairing Code and IP to UI
        Task.Run(async () => {
            await Task.Delay(3000); // Give UI time to fully load
            _controlManager!.NotifyWindowReady();
            _controlManager!.SendLocalIpToWeb();
        });

        window.WaitForClose();

        _controlManager.Stop();
    }

    private static void ProcessUiMessage(string message, PhotinoWindow window)
    {
        _controlManager?.NotifyWindowReady();
        Console.WriteLine($"[BACKEND] Received UI Message: {message}");
        try 
        {
            var doc = JsonDocument.Parse(message);
            string? type = doc.RootElement.GetProperty("type").GetString();
            Console.WriteLine($"[BACKEND] Message Type: {type}");

            switch (type)
            {
                case "start_discovery":
                    Console.WriteLine("[BACKEND] Refreshing discovery...");
                    _controlManager!.RefreshDiscovery();
                    var devices = _controlManager!.GetDevices();
                    window.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_result", devices }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                    break;
                case "service_toggle":
                    string service = doc.RootElement.GetProperty("service").GetString() ?? "";
                    bool enabled = doc.RootElement.GetProperty("enabled").GetBoolean();
                    _controlManager!.ToggleService(service, enabled);
                    break;
                case "connect_device":
                    string ipOrCode = doc.RootElement.TryGetProperty("ip", out var ipProp) ? ipProp.GetString() ?? "" : "";
                    if (!string.IsNullOrEmpty(ipOrCode))
                    {
                        _controlManager!.Connect(ipOrCode, window);
                    }
                    break;
                case "get_settings":
                    _controlManager!.SendSettingsToWeb();
                    break;
                case "update_settings":
                    string activeEdge = doc.RootElement.TryGetProperty("edge", out var edgeProp) ? edgeProp.GetString() ?? "Right" : "Right";
                    bool lockInput = doc.RootElement.TryGetProperty("lockInput", out var lockProp) ? lockProp.GetBoolean() : true;
                    int delay = doc.RootElement.TryGetProperty("delay", out var delayProp) ? delayProp.GetInt32() : 150;
                    int cornerSize = doc.RootElement.TryGetProperty("cornerSize", out var cornerProp) ? cornerProp.GetInt32() : 50;
                    double sensitivity = doc.RootElement.TryGetProperty("sensitivity", out var sensProp) ? sensProp.GetDouble() : 0.7;
                    int gestureThreshold = doc.RootElement.TryGetProperty("gestureThreshold", out var gestureProp) ? gestureProp.GetInt32() : 1000;
                    string? pairingCode = doc.RootElement.TryGetProperty("pairingCode", out var pinProp) ? pinProp.GetString() : null;
                    string? activeMonitor = doc.RootElement.TryGetProperty("activeMonitor", out var monitorProp) ? monitorProp.GetString() : null;
                    _controlManager!.UpdateSettings(activeEdge, lockInput, delay, cornerSize, sensitivity, gestureThreshold, pairingCode, activeMonitor);
                    break;
                case "reset_settings":
                    _controlManager!.ResetSettings();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UI Message Error: {ex.Message}");
        }
    }
}
