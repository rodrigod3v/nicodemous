using System;
using System.IO;
using System.Text.Json;
using Photino.NET;
using Nicodemous.Backend.Services;

namespace Nicodemous.Backend;

class Program
{
    private static UniversalControlManager? _controlManager;
    private static DiscoveryService? _discoveryService;

    [STAThread]
    static void Main(string[] args)
    {
        string windowTitle = "Nicodemous - Universal Control";
        string initialUrl = "http://localhost:5173"; 

        var window = new PhotinoWindow()
            .SetTitle(windowTitle)
            .SetUseOsDefaultSize(false)
            .SetSize(1200, 800)
            .Center()
            .SetResizable(true);

        // Initialize Central Manager
        _controlManager = new UniversalControlManager();
        _discoveryService = new DiscoveryService(Environment.MachineName, 8888);

        // UI Callbacks
        window.RegisterWebMessageReceivedHandler((object? sender, string message) => 
        {
            var windowRef = (PhotinoWindow)sender!;
            ProcessUiMessage(message, windowRef);
        });

        _controlManager.Start();
        
        window.Load(initialUrl);
        window.WaitForClose();

        _controlManager.Stop();
    }

    private static void ProcessUiMessage(string message, PhotinoWindow window)
    {
        try 
        {
            var doc = JsonDocument.Parse(message);
            string? type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "start_discovery":
                    Task.Run(async () => {
                        var devices = await _discoveryService!.Browse();
                        window.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_results", devices }));
                    });
                    break;
                case "service_toggle":
                    string service = doc.RootElement.GetProperty("service").GetString() ?? "";
                    bool enabled = doc.RootElement.GetProperty("enabled").GetBoolean();
                    _controlManager!.ToggleService(service, enabled);
                    break;
                case "connect_device":
                    string ip = doc.RootElement.GetProperty("ip").GetString() ?? "";
                    _controlManager!.ConnectTo(ip);
                    _controlManager!.SetRemoteControlState(true);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UI Message Error: {ex.Message}");
        }
    }
}
