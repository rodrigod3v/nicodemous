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

            switch (type)
            {
                case "mouse_move":
                    short x = doc.RootElement.GetProperty("x").GetInt16();
                    short y = doc.RootElement.GetProperty("y").GetInt16();
                    _simulator.SimulateMouseMovement(x, y);
                    break;

                case "mouse_click":
                    // Simplified: just press/release or click
                    string buttonStr = doc.RootElement.GetProperty("button").GetString() ?? "Left";
                    if (Enum.TryParse<MouseButton>(buttonStr, out var button))
                    {
                         _simulator.SimulateMousePress(button);
                         _simulator.SimulateMouseRelease(button);
                    }
                    break;

                case "key_press":
                    string keyStr = doc.RootElement.GetProperty("key").GetString() ?? "A";
                    if (Enum.TryParse<KeyCode>(keyStr, out var keyCode))
                    {
                        _simulator.SimulateKeyPress(keyCode);
                        _simulator.SimulateKeyRelease(keyCode);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Injection Error: {ex.Message}");
        }
    }
}
