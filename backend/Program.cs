using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
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
        string windowTitle = "nicodemouse";
        
#if DEBUG
        string initialUrl = "http://localhost:5173"; 
#else
        string initialUrl = "wwwroot/index.html";
#endif

        var window = new PhotinoWindow()
            .SetTitle(windowTitle)
            .SetChromeless(true)
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)
            .SetSize(1280, 850)
            .Center()
            .SetResizable(true)
#if DEBUG
            .SetContextMenuEnabled(true)
            .SetDevToolsEnabled(true);
#else
            .SetContextMenuEnabled(false)
            .SetDevToolsEnabled(false);
#endif

        // Robust Icon Loading
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        string iconFilename = isMac ? "logo_n.png" : "app_icon.ico";

        string[] potentialIconPaths = {
            Path.GetFullPath(Path.Combine(exeDir, "Assets", iconFilename)),
            Path.GetFullPath(Path.Combine(exeDir, "backend", "Assets", iconFilename)),
            Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "Assets", iconFilename)),
            Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "backend", "Assets", iconFilename))
        };

        string? iconPath = null;
        foreach (var path in potentialIconPaths)
        {
            if (File.Exists(path))
            {
                iconPath = path;
                break;
            }
        }

        if (iconPath != null)
        {
            window.SetIconFile(iconPath);
            Console.WriteLine($"[INFO] Application icon loaded from: {iconPath}");
        }
        else
        {
            Console.WriteLine("[ERROR] Application icon not found in any potential paths.");
        }

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
