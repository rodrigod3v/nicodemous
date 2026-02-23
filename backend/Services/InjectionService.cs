using SharpHook;
using SharpHook.Native;
using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class InjectionService
{
    private readonly IEventSimulator _simulator;

    public InjectionService()
    {
        _simulator = new EventSimulator();
    }

    private short _screenWidth = 1920;
    private short _screenHeight = 1080;

    public void SetScreenSize(short width, short height)
    {
        _screenWidth = width;
        _screenHeight = height;
    }

    public void Inject(string json)
    {
        try 
        {
            var doc = JsonDocument.Parse(json);
            string? type = doc.RootElement.GetProperty("type").GetString();

            if (type == "mouse_move")
            {
                ushort normX = doc.RootElement.GetProperty("x").GetUInt16();
                ushort normY = doc.RootElement.GetProperty("y").GetUInt16();
                InjectMouseMove(normX, normY);
            }
// ... rest of the switch logic
            else if (type == "mouse_click")
            {
                string button = doc.RootElement.GetProperty("button").GetString() ?? "Left";
                InjectMouseClick(button);
            }
            else if (type == "key_press")
            {
                string key = doc.RootElement.GetProperty("key").GetString() ?? "";
                InjectKeyPress(key);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Injection Error: {ex.Message}");
        }
    }

    public void InjectMouseMove(ushort normX, ushort normY)
    {
        // Convert normalized percentages (0-65535) back to pixel coordinates
        short x = (short)((double)normX / 65535 * _screenWidth);
        short y = (short)((double)normY / 65535 * _screenHeight);
        _simulator.SimulateMouseMovement(x, y);
    }

    public void InjectMouseClick(string buttonStr)
    {
        var assembly = typeof(SharpHook.IEventSimulator).Assembly;
        var mouseButtonType = assembly.GetType("SharpHook.Native.MouseButton");
        if (mouseButtonType != null)
        {
            var button = Enum.Parse(mouseButtonType, buttonStr == "Left" || buttonStr == "Button1" ? "Button1" : "Button2");
            ((dynamic)_simulator).SimulateMousePress((dynamic)button);
            ((dynamic)_simulator).SimulateMouseRelease((dynamic)button);
        }
    }

    public void InjectKeyPress(string keyStr)
    {
        var assembly = typeof(SharpHook.IEventSimulator).Assembly;
        var keyCodeType = assembly.GetType("SharpHook.Native.KeyCode");
        if (keyCodeType != null)
        {
            string mappedKey = keyStr.Length == 1 ? "Vc" + keyStr.ToUpper() : keyStr;
            var keyCode = Enum.Parse(keyCodeType, mappedKey);
            ((dynamic)_simulator).SimulateKeyPress((dynamic)keyCode);
            ((dynamic)_simulator).SimulateKeyRelease((dynamic)keyCode);
        }
    }
}
