using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Reflection;
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
        // Global exception handlers for background threads and tasks
        AppDomain.CurrentDomain.UnhandledException += (s, e) => 
            LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        
        TaskScheduler.UnobservedTaskException += (s, e) => 
        {
            LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        try 
        {
            Console.WriteLine($"[BOOT] Starting nicodemouse on {RuntimeInformation.OSDescription}");
            RunApp(args);
        }
        catch (Exception ex)
        {
            LogCrash("Main.Catch", ex);
            throw; // Re-throw to let the OS handle the final crash if needed
        }
    }

    private static void LogCrash(string source, Exception? ex)
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logDir = Path.Combine(appData, "nicodemouse");
            Directory.CreateDirectory(logDir);
            string logPath = Path.Combine(logDir, "crash.log");

            string message = $"[{DateTime.Now}] --- CRASH REPORT --- \nSource: {source}\nTime: {DateTime.Now}\nException: {ex?.ToString() ?? "Unknown"}\n\n";
            File.AppendAllText(logPath, message);
            
            Console.WriteLine("**************************************************");
            Console.WriteLine($"CRITICAL ERROR detected in {source}");
            Console.WriteLine(ex?.ToString());
            Console.WriteLine($"A detailed report has been saved to: {logPath}");
            Console.WriteLine("**************************************************");
        }
        catch (Exception logEx)
        {
            Console.WriteLine($"[FATAL] Could not even write the crash log: {logEx.Message}");
        }
    }

    static void RunApp(string[] args)
    {
        string windowTitle = "nicodemouse";
        
        string exeDir = AppContext.BaseDirectory;

#if DEBUG
        string initialUrl = "http://localhost:5173"; 
#else
        string initialUrl = "http://nicodemouse.local/index.html";
#endif

        var window = new PhotinoWindow()
            .SetTitle(windowTitle)
            .SetChromeless(true)
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)
            .SetSize(1280, 850)
            .Center()
            .SetResizable(true)
            .RegisterCustomSchemeHandler("http", (object sender, string scheme, string url, out string contentType) =>
            {
                contentType = "text/html";
                
                // ONLY intercept requests to our local domain
                if (!url.StartsWith("http://nicodemouse.local", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = null;
                    return null;
                }

                var cleanUrl = url.Replace("http://nicodemouse.local/", "").Replace("http://nicodemouse.local", "").Split('?')[0].Split('#')[0];
                if (string.IsNullOrWhiteSpace(cleanUrl) || cleanUrl == "/") cleanUrl = "index.html";
                if (cleanUrl.StartsWith("/")) cleanUrl = cleanUrl.Substring(1);

                if (cleanUrl.EndsWith(".js")) contentType = "text/javascript";
                else if (cleanUrl.EndsWith(".css")) contentType = "text/css";
                else if (cleanUrl.EndsWith(".png")) contentType = "image/png";
                else if (cleanUrl.EndsWith(".svg")) contentType = "image/svg+xml";
                else if (cleanUrl.EndsWith(".ico")) contentType = "image/x-icon";
                else if (cleanUrl.EndsWith(".woff") || cleanUrl.EndsWith(".woff2")) contentType = "font/woff2";

                string dotPath = cleanUrl.Replace("/", ".").Replace("\\", ".");
                string resourceName = "nicodemouse_backend.wwwroot." + dotPath;
                
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream(resourceName);
                
                if (stream == null && cleanUrl == "index.html") 
                {
                    stream = assembly.GetManifestResourceStream("nicodemouse_backend.wwwroot.index.html");
                }
                
                return stream;
            })
            .SetContextMenuEnabled(true)
            .SetDevToolsEnabled(true);

        window.StartUrl = initialUrl;
        window.StartString = "<html></html>";

        // Robust Icon Loading
        bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        string iconFilename = isMac ? "logo_n.png" : "app_icon.ico";

#if !DEBUG
        var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"nicodemouse_backend.Assets.{iconFilename}");
        if (iconStream != null)
        {
            var tempIconPath = Path.Combine(Path.GetTempPath(), $"nicodemouse_temp_{iconFilename}");
            using (var fileStream = File.Create(tempIconPath))
            {
                iconStream.CopyTo(fileStream);
            }
            window.SetIconFile(tempIconPath);
            Console.WriteLine($"[INFO] Application icon loaded from embedded resource to: {tempIconPath}");
        }
        else
        {
            Console.WriteLine($"[ERROR] Application embedded icon not found: nicodemouse_backend.Assets.{iconFilename}");
        }
#else
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
        }
#endif

        // Initialize Central Manager
        var settings = new SettingsService();
        _controlManager = new UniversalControlManager(settings);
        _controlManager.SetWindow(window);

        // Initialize UI Message Handler
        _uiHandler = new UiMessageHandler(_controlManager, window);

        // Initialize Tray Support
        try 
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] TrayService initialization failed: {ex.Message}");
            // Continue app even if tray fails
            window.Load(initialUrl);
            window.WaitForClose();
        }
    }
}
