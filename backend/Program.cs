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
        _controlManager = new UniversalControlManager();
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
            window.SendWebMessage(JsonSerializer.Serialize(new { 
                type = "local_ip", 
                ip = _controlManager.PairingCode 
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
                case "update_settings":
                    string activeEdge = doc.RootElement.GetProperty("edge").GetString() ?? "Right";
                    bool lockInput = doc.RootElement.GetProperty("lockInput").GetBoolean();
                    _controlManager!.UpdateSettings(activeEdge, lockInput);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UI Message Error: {ex.Message}");
        }
    }
}
