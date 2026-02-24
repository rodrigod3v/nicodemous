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
    private readonly ClipboardService _clipboardService;
    private short _screenWidth  = 1920;
    private short _screenHeight = 1080;

    public InjectionService(ClipboardService clipboardService)
    {
        _simulator        = new EventSimulator();
        _clipboardService = clipboardService;
    }

    public void SetScreenSize(short w, short h)
    {
        _screenWidth = w;
        _screenHeight = h;
    }

    // -----------------------------------------------------------------------
    // Clipboard
    // -----------------------------------------------------------------------

    /// <summary>
    /// Writes <paramref name="text"/> to the local clipboard then simulates
    /// the platform-appropriate paste shortcut so the focused app receives it.
    /// Windows: Ctrl+V / macOS: Cmd+V (Meta+V via SharpHook VcLeftMeta).
    /// </summary>
    public void InjectClipboardAndPaste(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        try
        {
            // 1. Write text to clipboard of this machine
            _clipboardService.SetText(text);

            // 2. Simulate the paste shortcut
            // Using Task.Delay.Wait is okay here as we are in a background task managed by UniversalControlManager
            Task.Delay(100).Wait(); // increased delay slightly for macOS stability

            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            KeyCode modifier = isMac ? KeyCode.VcLeftMeta : KeyCode.VcLeftControl;

            _simulator.SimulateKeyPress(modifier);
            _simulator.SimulateKeyPress(KeyCode.VcV);
            _simulator.SimulateKeyRelease(KeyCode.VcV);
            _simulator.SimulateKeyRelease(modifier);

            Console.WriteLine($"[INJECT] Clipboard paste injected ({text.Length} chars, {(isMac ? "Cmd" : "Ctrl")}+V)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INJECT] InjectClipboardAndPaste error: {ex.Message}");
        }
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Optimization: use native relative move to avoid polling lag
                mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, UIntPtr.Zero);
                return;
            }

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

    private const uint MOUSEEVENTF_MOVE = 0x0001;

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

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
            if (evt == IntPtr.Zero) 
            {
                // This happens if accessibility permissions are not granted.
                return (0, 0);
            }

            var pt = CGEventGetLocation(evt);
            CFRelease(evt);
            return ((int)pt.X, (int)pt.Y);
        }
        catch { return (0, 0); }
    }
}
