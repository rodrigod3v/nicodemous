using SharpHook;
using SharpHook.Native;
using SharpHook.Data;
using System;
using System.Text.Json;
using System.Threading;

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
            else if (type == "mouse_click")
            {
                string button = doc.RootElement.GetProperty("button").GetString() ?? "Left";
                InjectMouseClick(button);
            }
            else if (type == "mouse_down")
            {
                string button = doc.RootElement.GetProperty("button").GetString() ?? "Left";
                InjectMouseDown(button);
            }
            else if (type == "mouse_up")
            {
                string button = doc.RootElement.GetProperty("button").GetString() ?? "Left";
                InjectMouseUp(button);
            }
            else if (type == "mouse_wheel")
            {
                short delta = doc.RootElement.GetProperty("delta").GetInt16();
                InjectMouseWheel(delta);
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
        try 
        {
            // Convert normalized percentages (0-65535) back to pixel coordinates
            // Use (size - 1) to ensure we can hit the exact edge pixel (e.g., 1079 for 1080p)
            short x = (short)((double)normX / 65535 * (_screenWidth - 1));
            short y = (short)((double)normY / 65535 * (_screenHeight - 1));
            
            // Log occasionally to avoid spam but confirm activity
            if (normX % 2000 == 0) Console.WriteLine($"[INJECT] Mouse Move to {x},{y} (Screen: {_screenWidth}x{_screenHeight})");
            
            _simulator.SimulateMouseMovement(x, y);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] Mouse Move Error: {ex.Message}");
        }
    }

    public void InjectMouseClick(string buttonStr)
    {
        try 
        {
            Console.WriteLine($"[INJECT] Mouse Click: {buttonStr}");
            InjectMouseDown(buttonStr);
            Thread.Sleep(50); 
            InjectMouseUp(buttonStr);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] Click Error: {ex.Message}");
        }
    }

    public void InjectMouseDown(string buttonStr)
    {
        try 
        {
            var button = ParseButton(buttonStr);
            Console.WriteLine($"[INJECT] Mouse Down: {button}");
            _simulator.SimulateMousePress(button);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] Mouse Down Error: {ex.Message}");
        }
    }

    public void InjectMouseUp(string buttonStr)
    {
        try 
        {
            var button = ParseButton(buttonStr);
            Console.WriteLine($"[INJECT] Mouse Up: {button}");
            _simulator.SimulateMouseRelease(button);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] Mouse Up Error: {ex.Message}");
        }
    }

    public void InjectMouseWheel(short delta)
    {
        try 
        {
            Console.WriteLine($"[INJECT] Mouse Wheel: {delta}");
            _simulator.SimulateMouseWheel(delta);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] Mouse Wheel Error: {ex.Message}");
        }
    }

    private MouseButton ParseButton(string buttonStr)
    {
        if (buttonStr == "Left" || buttonStr == "Button1") return MouseButton.Button1;
        if (buttonStr == "Right" || buttonStr == "Button2") return MouseButton.Button2;
        if (buttonStr == "Middle" || buttonStr == "Button3") return MouseButton.Button3;
        
        if (Enum.TryParse<MouseButton>(buttonStr, out var result)) return result;
        return MouseButton.Button1;
    }

    public void InjectKeyPress(string keyStr)
    {
        try 
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
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] Key Error: {ex.Message}");
        }
    }
}
