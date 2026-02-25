using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Photino.NET;
using nicodemouse.Backend.Services;
using nicodemouse.Backend.Handlers;

namespace nicodemouse.Backend;

class Program
{
    private static UniversalControlManager? _controlManager;
    private static UiMessageHandler? _uiHandler;

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

        // Initialize UI Message Handler
        _uiHandler = new UiMessageHandler(_controlManager, window);

        // Initialize Tray Support
        using var trayService = new TrayService(window, _controlManager);

        // UI Callbacks
        window.RegisterWebMessageReceivedHandler((object? sender, string message) => 
        {
            _ = _uiHandler.HandleMessageAsync(message);
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
}
