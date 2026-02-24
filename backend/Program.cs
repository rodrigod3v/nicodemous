using System;
using System.IO;
using System.Text.Json;
using Photino.NET;
using Nicodemous.Backend.Services;

namespace Nicodemous.Backend;

class Program
{
    private static UniversalControlManager? _controlManager;

    [STAThread]
    static void Main(string[] args)
    {
        string windowTitle = "Nicodemous - Universal Control";
        
#if DEBUG
        string initialUrl = "http://localhost:5173"; 
#else
        string initialUrl = "wwwroot/index.html";
#endif

        var window = new PhotinoWindow()
            .SetTitle(windowTitle)
            .SetUseOsDefaultSize(false)
            .SetSize(1200, 800)
            .Center()
            .SetResizable(true);

        // Initialize Central Manager
        var settings = new SettingsService();
        _controlManager = new UniversalControlManager(settings);
        _controlManager.SetWindow(window);

        // UI Callbacks
        window.RegisterWebMessageReceivedHandler((object? sender, string message) => 
        {
            var windowRef = (PhotinoWindow)sender!;
            ProcessUiMessage(message, windowRef);
        });

        _controlManager.Start();
        
        window.Load(initialUrl);

        // Send actual Pairing Code to UI
        Task.Run(async () => {
            await Task.Delay(4000); // Give UI time to fully load
            // Get local IP for display
            string localIp = "Unknown";
            try {
                using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0)) {
                    socket.Connect("8.8.8.8", 65530);
                    localIp = (socket.LocalEndPoint as System.Net.IPEndPoint)?.Address.ToString() ?? "Unknown";
                }
            } catch { /* Fallback to Unknown */ }

            window.SendWebMessage(JsonSerializer.Serialize(new { 
                type = "local_ip", 
                detail = new {
                    ip = localIp,
                    code = _controlManager.PairingCode
                }
            }));
        });

        window.WaitForClose();

        _controlManager.Stop();
    }

    private static void ProcessUiMessage(string message, PhotinoWindow window)
    {
        Console.WriteLine($"[BACKEND] Received UI Message: {message}");
        try 
        {
            var doc = JsonDocument.Parse(message);
            string? type = doc.RootElement.GetProperty("type").GetString();
            Console.WriteLine($"[BACKEND] Message Type: {type}");

            switch (type)
            {
                case "start_discovery":
                    Console.WriteLine("[BACKEND] Starting device discovery...");
                    var devices = _controlManager!.GetDevices();
                    window.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_result", devices }));
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
                    string settingsJson = _controlManager!.GetSettingsJson();
                    window.SendWebMessage(JsonSerializer.Serialize(new { type = "settings_data", settings = settingsJson }));
                    break;
                case "update_settings":
                    string activeEdge = doc.RootElement.GetProperty("edge").GetString() ?? "Right";
                    bool lockInput = doc.RootElement.GetProperty("lockInput").GetBoolean();
                    int delay = doc.RootElement.TryGetProperty("delay", out var delayProp) ? delayProp.GetInt32() : 150;
                    int cornerSize = doc.RootElement.TryGetProperty("cornerSize", out var cornerProp) ? cornerProp.GetInt32() : 50;
                    double sensitivity = doc.RootElement.TryGetProperty("sensitivity", out var sensProp) ? sensProp.GetDouble() : 0.7;
                    _controlManager!.UpdateSettings(activeEdge, lockInput, delay, cornerSize, sensitivity);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UI Message Error: {ex.Message}");
        }
    }
}
