using System;
using System.IO;
using System.Text.Json;

namespace nicodemouse.Backend.Services;

public class AppSettings
{
    public string PairingCode { get; set; } = "123456";
    public string ActiveEdge { get; set; } = "Right"; // "Left", "Right", "Top", "Bottom"
    public int SwitchingDelayMs { get; set; } = 150;
    public int DeadCornerSize { get; set; } = 50;
    public double MouseSensitivity { get; set; } = 1.0;
    public int GestureThreshold { get; set; } = 1000;
    public bool LockInput { get; set; } = true;
    public bool EnableInput { get; set; } = true;
    public bool EnableAudio { get; set; } = false;
    public bool EnableClipboard { get; set; } = true;
    public string ActiveMonitor { get; set; } = "";
}

public class SettingsService
{
    private readonly string _filePath;
    private AppSettings _settings;

    public SettingsService()
    {
        // Store settings in the same folder as the executable for simplicity, or appdata
        string folder = AppContext.BaseDirectory;
        _filePath = Path.Combine(folder, "settings.json");
        _settings = Load();
    }

    public AppSettings GetSettings() => _settings;

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SETTINGS] Error loading settings: {ex.Message}");
        }

        // Return defaults if file doesn't exist or is corrupt
        _settings = new AppSettings();
        Save(); // Create the file
        return _settings;
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            Console.WriteLine($"[SETTINGS] Saved to {_filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SETTINGS] Error saving settings: {ex.Message}");
        }
    }

    public void UpdateSettings(AppSettings newSettings)
    {
        _settings = newSettings;
        Save();
    }
}
