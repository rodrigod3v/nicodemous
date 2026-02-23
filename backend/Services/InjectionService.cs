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

    public void Inject(string json)
    {
        try 
        {
            var doc = JsonDocument.Parse(json);
            string? type = doc.RootElement.GetProperty("type").GetString();

            if (type == "mouse_move")
            {
                short x = doc.RootElement.GetProperty("x").GetInt16();
                short y = doc.RootElement.GetProperty("y").GetInt16();
                _simulator.SimulateMouseMovement(x, y);
                return;
            }

            // Using reflection to resolve SharpHook enums at runtime to bypass compile-time environment issues
            var assembly = typeof(SharpHook.IEventSimulator).Assembly;

            if (type == "mouse_click")
            {
                string buttonStr = doc.RootElement.GetProperty("button").GetString() ?? "Button1";
                var mouseButtonType = assembly.GetType("SharpHook.Native.MouseButton");
                if (mouseButtonType != null)
                {
                    var button = Enum.Parse(mouseButtonType, buttonStr == "Left" ? "Button1" : "Button2");
                    ((dynamic)_simulator).SimulateMousePress((dynamic)button);
                    ((dynamic)_simulator).SimulateMouseRelease((dynamic)button);
                }
            }
            else if (type == "key_press")
            {
                string keyStr = doc.RootElement.GetProperty("key").GetString() ?? "VcA";
                var keyCodeType = assembly.GetType("SharpHook.Native.KeyCode");
                if (keyCodeType != null)
                {
                    // Basic mapping for alphanumeric keys
                    string mappedKey = keyStr.Length == 1 ? "Vc" + keyStr.ToUpper() : keyStr;
                    var keyCode = Enum.Parse(keyCodeType, mappedKey);
                    ((dynamic)_simulator).SimulateKeyPress((dynamic)keyCode);
                    ((dynamic)_simulator).SimulateKeyRelease((dynamic)keyCode);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Injection Error: {ex.Message}");
        }
    }
}
