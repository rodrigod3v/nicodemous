using SharpHook;
using SharpHook.Native;
using SharpHook.Data;
using System.Runtime.InteropServices;

namespace Nicodemous.Backend.Services;

/// <summary>
/// Injects synthesized mouse and keyboard events on the receiving (secondary) machine.
/// Uses KeyMap for stable KeyID ↔ SharpHook KeyCode translation — no reflection.
/// Relative mouse movement uses GetCursorPos (Windows) to find current cursor,
/// then moves delta from that position.
/// </summary>
public class InjectionService
{
    private readonly IEventSimulator _simulator;
    private short _screenWidth  = 1920;
    private short _screenHeight = 1080;

    public InjectionService()
    {
        _simulator = new EventSimulator();
    }

    public void SetScreenSize(short w, short h)
    {
        _screenWidth = w;
        _screenHeight = h;
    }

    // -----------------------------------------------------------------------
    // Mouse — Relative Movement
    // -----------------------------------------------------------------------

    /// <summary>
    /// Applies relative delta to the current cursor position.
    /// Clamps to screen bounds.
    /// </summary>
    public void InjectMouseRelMove(short dx, short dy)
    {
        try
        {
            var (cx, cy) = GetCurrentCursorPos();
            int newX = Math.Clamp(cx + dx, 0, _screenWidth  - 1);
            int newY = Math.Clamp(cy + dy, 0, _screenHeight - 1);
            _simulator.SimulateMouseMovement((short)newX, (short)newY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] RelMove error: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Mouse — Absolute Movement (kept for initial screen-enter positioning)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Positions the cursor at an absolute normalized coordinate (0-65535 range).
    /// </summary>
    public void InjectMouseMove(ushort normX, ushort normY)
    {
        try
        {
            short x = (short)((double)normX / 65535 * (_screenWidth  - 1));
            short y = (short)((double)normY / 65535 * (_screenHeight - 1));
            _simulator.SimulateMouseMovement(x, y);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] AbsMove error: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Mouse — Buttons
    // -----------------------------------------------------------------------

    public void InjectMouseDown(byte buttonId)
    {
        try
        {
            var btn = ButtonFromId(buttonId);
            _simulator.SimulateMousePress(btn);
        }
        catch (Exception ex) { Console.WriteLine($"[INJECT] MouseDown error: {ex.Message}"); }
    }

    public void InjectMouseUp(byte buttonId)
    {
        try
        {
            var btn = ButtonFromId(buttonId);
            _simulator.SimulateMouseRelease(btn);
        }
        catch (Exception ex) { Console.WriteLine($"[INJECT] MouseUp error: {ex.Message}"); }
    }

    // -----------------------------------------------------------------------
    // Mouse — Wheel
    // -----------------------------------------------------------------------

    /// <summary>
    /// Injects a wheel scroll event.
    /// delta follows Input Leap convention: +120 = one notch forward/up, -120 = one notch back/down.
    /// SharpHook SimulateMouseWheel expects the same ±120 unit, so we pass through directly.
    /// </summary>
    public void InjectMouseWheel(short xDelta, short yDelta)
    {
        try
        {
            if (yDelta != 0) _simulator.SimulateMouseWheel(yDelta);
            // Horizontal scroll: SharpHook doesn't expose a public horizontal API directly;
            // if needed in future, can use WinAPI SendInput with MOUSEEVENTF_HWHEEL.
        }
        catch (Exception ex) { Console.WriteLine($"[INJECT] Wheel error: {ex.Message}"); }
    }

    // -----------------------------------------------------------------------
    // Keyboard
    // -----------------------------------------------------------------------

    public void InjectKeyDown(ushort keyId, ushort modifiers)
    {
        // Apply modifier keys first, then the main key
        ApplyModifiers(modifiers, press: true);

        if (KeyMap.IdToKeyCode.TryGetValue(keyId, out var code))
        {
            try { _simulator.SimulateKeyPress(code); }
            catch (Exception ex) { Console.WriteLine($"[INJECT] KeyDown error (keyId={keyId:X4}): {ex.Message}"); }
        }
        else
        {
            Console.WriteLine($"[INJECT] Unknown keyId {keyId:X4} — skipping.");
        }
    }

    public void InjectKeyUp(ushort keyId, ushort modifiers)
    {
        if (KeyMap.IdToKeyCode.TryGetValue(keyId, out var code))
        {
            try { _simulator.SimulateKeyRelease(code); }
            catch (Exception ex) { Console.WriteLine($"[INJECT] KeyUp error (keyId={keyId:X4}): {ex.Message}"); }
        }

        // Release modifier keys after the main key
        ApplyModifiers(modifiers, press: false);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void ApplyModifiers(ushort modifiers, bool press)
    {
        void Act(KeyCode kc)
        {
            try
            {
                if (press) _simulator.SimulateKeyPress(kc);
                else        _simulator.SimulateKeyRelease(kc);
            }
            catch { /* ignore individual modifier errors */ }
        }

        if ((modifiers & KeyMap.ModShift)   != 0) Act(KeyCode.VcLeftShift);
        if ((modifiers & KeyMap.ModControl) != 0) Act(KeyCode.VcLeftControl);
        if ((modifiers & KeyMap.ModAlt)     != 0) Act(KeyCode.VcLeftAlt);
        if ((modifiers & KeyMap.ModMeta)    != 0) Act(KeyCode.VcLeftMeta);
    }

    private static MouseButton ButtonFromId(byte id) => id switch
    {
        1 => MouseButton.Button1, // Left
        2 => MouseButton.Button2, // Right
        3 => MouseButton.Button3, // Middle
        4 => MouseButton.Button4,
        5 => MouseButton.Button5,
        _ => MouseButton.Button1,
    };

    // -----------------------------------------------------------------------
    // Platform cursor position
    // -----------------------------------------------------------------------

    private (int x, int y) GetCurrentCursorPos()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetCursorPosWindows();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetCursorPosMacOS();

        return (_screenWidth / 2, _screenHeight / 2);
    }

    // --- Windows ---
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    private static (int x, int y) GetCursorPosWindows()
    {
        if (GetCursorPos(out var pt)) return (pt.X, pt.Y);
        return (0, 0);
    }

    // --- macOS (CoreGraphics) ---
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventCreate(IntPtr src);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern CGPoint CGEventGetLocation(IntPtr evt);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr obj);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint { public double X; public double Y; }

    private static (int x, int y) GetCursorPosMacOS()
    {
        try
        {
            IntPtr evt = CGEventCreate(IntPtr.Zero);
            var pt = CGEventGetLocation(evt);
            CFRelease(evt);
            return ((int)pt.X, (int)pt.Y);
        }
        catch { return (0, 0); }
    }
}
