using System;
using System.IO;
using System.Text.Json;
using PhotinoNET;
using Nicodemous.Backend.Services;

namespace Nicodemous.Backend;

class Program
{
    private static InputService? _inputService;
    private static DiscoveryService? _discoveryService;

    [STAThread]
    static void Main(string[] args)
    {
        // Window title and URL
        string windowTitle = "Nicodemous - Universal Control";
        
        // Use development URL if in debug or just point to a placeholder for now
        // We will configure the React dev server URL later.
        string initialUrl = "http://localhost:5173"; 

        var window = new PhotinoWindow()
            .SetTitle(windowTitle)
            .SetUseOsDefaultSize(false)
            .SetSize(1200, 800)
            .SetCenterOnScreen(true)
            .SetResizable(true)
            .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
            {
                contentType = "text/javascript";
                return new MemoryStream();
            });

        // Initialize Services
        _inputService = new InputService((json) => 
        {
            // Send events to UI if needed, but primary use is P2P transmission
            // window.SendWebMessage(json); 
        });

        _discoveryService = new DiscoveryService(Environment.MachineName, 8888);

        // UI Callbacks
        window.RegisterWebMessageReceivedHandler((object? sender, string message) => 
        {
            var windowRef = (PhotinoWindow)sender!;
            ProcessUiMessage(message, windowRef);
        });

        _inputService.Start();
        
        window.Load(initialUrl);
        window.WaitForClose();

        _inputService.Stop();
    }

    private static void ProcessUiMessage(string message, PhotinoWindow window)
    {
        try 
        {
            var doc = JsonDocument.Parse(message);
            string? command = doc.RootElement.GetProperty("command").GetString();

            switch (command)
            {
                case "start_discovery":
                    Task.Run(async () => {
                        var devices = await _discoveryService!.Browse();
                        window.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_results", devices }));
                    });
                    break;
                case "toggle_input":
                    // Logic to enable/disable local capture
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
        }
    }
}
