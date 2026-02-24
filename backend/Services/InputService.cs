using SharpHook;
using SharpHook.Native;
using SharpHook.Data;
using System.Runtime.InteropServices;

namespace Nicodemous.Backend.Services;

public enum ScreenEdge { None, Left, Right, Top, Bottom }

/// <summary>
/// Captures mouse and keyboard input locally.
/// 
/// In REMOTE MODE:
///   - Mouse movement is sent as relative deltas (dx/dy), inspired by Input Leap's kMsgDMouseRelMove.
///     The physical cursor is locked to a sticky point at the active screen edge.
///   - Mouse buttons are sent as separate Down/Up packets (byte ButtonID: 1=Left, 2=Right, 3=Middle).
///   - Mouse wheel is sent in ±120 per-notch units on two axes (X and Y), matching Input Leap convention.
///   - Keys are sent as KeyDown/KeyUp with a stable platform-agnostic KeyID and a modifier mask.
///     Modifier state is tracked so every key event carries the current Shift/Ctrl/Alt/Meta state.
/// 
/// In LOCAL MODE:
///   - Monitors for edge crossings to trigger remote mode.
/// </summary>
public class InputService : IDisposable
{
    private readonly SimpleGlobalHook _hook;
    private readonly IEventSimulator _simulator;
    private readonly Action<byte[]> _onData;

    private bool _isRemoteMode = false;
    private short _screenWidth  = 1920;
    private short _screenHeight = 1080;
    private ScreenEdge _activeEdge = ScreenEdge.Right;
    private bool _isInputLocked = true;

    // Sticky-point cursor lock
    private bool _isSuppressingEvents = false;
    private short _lastRawX, _lastRawY; // Last raw position before suppression
    private double _accumulatedReturnDelta = 0;
    private DateTime _lastReturnAccumulateTime = DateTime.MinValue;
    private const int ReturnThreshold        = 1500; // px of deliberate push needed to return
    private const int ReturnDecayMs          = 300;  // accumulator resets if no push for this long
    private DateTime _lastReturnTime = DateTime.MinValue;
    private const int CooldownMs = 800;
    private double _accumDx = 0, _accumDy = 0; // Sub-pixel accumulation

    // Modifier tracking
    private bool _shiftDown, _ctrlDown, _altDown, _metaDown;

    // Switching delay
    public int SwitchDelayMs { get; set; } = 150; // ms to hold at edge before switching
    public int CornerSize { get; set; } = 50;    // Ignore edge hits within X px of a corner
    public double MouseSensitivity { get; set; } = 1.0;
    private DateTime _edgeHitStartTime = DateTime.MinValue;
    private ScreenEdge _lastDetectedEdge = ScreenEdge.None;

    // Entry Y position (so the remote cursor lands at the same height)
    private double _entryVirtualY;

    public event Action<ScreenEdge>? OnEdgeHit;
    public event Action? OnReturn;

    // -----------------------------------------------------------------------
    // Configuration API
    // -----------------------------------------------------------------------

    public void SetActiveEdge(string edge) =>
        _activeEdge = Enum.TryParse<ScreenEdge>(edge, true, out var r) ? r : ScreenEdge.Right;

    public void SetInputLock(bool locked) =>
        _isInputLocked = locked;

    public void SetScreenSize(short w, short h)
    {
        _screenWidth = w;
        _screenHeight = h;
    }

    // -----------------------------------------------------------------------
    // Construction & lifecycle
    // -----------------------------------------------------------------------

    public InputService(Action<byte[]> onData)
    {
        _hook = new SimpleGlobalHook();
        _simulator = new EventSimulator();
        _onData = onData;

        _hook.MouseMoved    += OnMouseMoved;
        _hook.MouseDragged  += OnMouseMoved;  // Also fires during button-held moves
        _hook.MousePressed  += OnMousePressed;
        _hook.MouseReleased += OnMouseReleased;
        _hook.MouseWheel    += OnMouseWheel;
        _hook.KeyPressed    += OnKeyPressed;
        _hook.KeyReleased   += OnKeyReleased;
    }

    public void Start() => Task.Run(() => _hook.Run());

    public void Stop() => _hook.Dispose();

    public void Dispose() => Stop();

    // -----------------------------------------------------------------------
    // Remote mode control
    // -----------------------------------------------------------------------

    public void SetRemoteMode(bool enabled)
    {
        _isRemoteMode = enabled;
        _accumulatedReturnDelta = 0;

        if (enabled)
        {
            // Park the cursor at the sticky point immediately
            short stickyX = GetStickyX();
            short stickyY = (short)(_screenHeight / 2);
            _lastRawX = stickyX;
            _lastRawY = stickyY;

            _isSuppressingEvents = true;
            _simulator.SimulateMouseMovement(stickyX, stickyY);
            _isSuppressingEvents = false;

            Console.WriteLine($"[INPUT] Remote mode ON. Sticky at ({stickyX},{stickyY}). Screen {_screenWidth}x{_screenHeight}.");
        }
        else
        {
            // Release any held modifier keys on the remote before leaving
            // (the receiver will handle this via its own key-up events)
            Console.WriteLine("[INPUT] Remote mode OFF.");
        }
    }

    // -----------------------------------------------------------------------
    // Mouse Events
    // -----------------------------------------------------------------------

    private void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        if (_isSuppressingEvents) return;

        if (_isRemoteMode)
        {
            e.SuppressEvent = true;
            HandleMouseLockAndSendDelta(e.Data.X, e.Data.Y);
        }
        else
        {
            if ((DateTime.Now - _lastReturnTime).TotalMilliseconds < CooldownMs) return;
            CheckEdge(e.Data.X, e.Data.Y);
        }
    }

    private void HandleMouseLockAndSendDelta(short rawX, short rawY)
    {
        if (!_isInputLocked)
        {
            // Free-roam mode (no locking): just send the absolute delta
            // relative to the last reported position
            _accumDx += (rawX - _lastRawX) * MouseSensitivity;
            _accumDy += (rawY - _lastRawY) * MouseSensitivity;
            _lastRawX = rawX;
            _lastRawY = rawY;

            short freeDx = (short)_accumDx;
            short freeDy = (short)_accumDy;

            if (freeDx != 0 || freeDy != 0)
            {
                _accumDx -= freeDx;
                _accumDy -= freeDy;
                _onData(PacketSerializer.SerializeMouseRelMove(freeDx, freeDy));
            }
            return;
        }

        // Locked mode: cursor is stuck at the sticky point.
        // Calculate delta from sticky center and send it, then warp cursor back.
        short stickyX = GetStickyX();
        short stickyY = (short)(_screenHeight / 2);

        _accumDx += (rawX - stickyX) * MouseSensitivity;
        _accumDy += (rawY - stickyY) * MouseSensitivity;

        short dx = (short)_accumDx;
        short dy = (short)_accumDy;

        if (dx == 0 && dy == 0) return;

        // "Consume" the integer pixels we are sending
        _accumDx -= dx;
        _accumDy -= dy;

        // Accumulate return gesture (moving back deliberately towards the home screen).
        // The accumulator decays if the user stops pushing for ReturnDecayMs, preventing
        // accidental exits during normal remote usage.
        bool movingBack = (_activeEdge == ScreenEdge.Right && dx < 0) ||
                          (_activeEdge == ScreenEdge.Left  && dx > 0);

        if (movingBack)
        {
            // Time-based decay: if too much time passed since last accumulation, reset first
            if ((DateTime.Now - _lastReturnAccumulateTime).TotalMilliseconds > ReturnDecayMs)
                _accumulatedReturnDelta = 0;

            _accumulatedReturnDelta += Math.Abs(dx);
            _lastReturnAccumulateTime = DateTime.Now;

            if (_accumulatedReturnDelta >= ReturnThreshold)
            {
                Console.WriteLine($"[INPUT] Return gesture detected (accumulated {_accumulatedReturnDelta}px).");
                _lastReturnTime = DateTime.Now;
                _accumulatedReturnDelta = 0;
                OnReturn?.Invoke();
                return;
            }
        }
        else
        {
            // Reset accumulator if user moves towards remote screen
            _accumulatedReturnDelta = 0;
        }

        // Send delta to remote
        _onData(PacketSerializer.SerializeMouseRelMove(dx, dy));

        // Warp physical cursor back to sticky point
        _isSuppressingEvents = true;
        _simulator.SimulateMouseMovement(stickyX, stickyY);
        _isSuppressingEvents = false;
    }

    private void CheckEdge(short x, short y)
    {
        ScreenEdge currentEdge = ScreenEdge.None;

        if (_activeEdge == ScreenEdge.Right && x >= _screenWidth - 1)
        {
            if (y >= CornerSize && y <= _screenHeight - CornerSize) currentEdge = ScreenEdge.Right;
        }
        else if (_activeEdge == ScreenEdge.Left && x <= 0)
        {
            if (y >= CornerSize && y <= _screenHeight - CornerSize) currentEdge = ScreenEdge.Left;
        }
        else if (_activeEdge == ScreenEdge.Top && y <= 0)
        {
            if (x >= CornerSize && x <= _screenWidth - CornerSize) currentEdge = ScreenEdge.Top;
        }
        else if (_activeEdge == ScreenEdge.Bottom && y >= _screenHeight - 1)
        {
            if (x >= CornerSize && x <= _screenWidth - CornerSize) currentEdge = ScreenEdge.Bottom;
        }

        // Delay logic
        if (currentEdge != ScreenEdge.None)
        {
            if (currentEdge != _lastDetectedEdge)
            {
                _lastDetectedEdge = currentEdge;
                _edgeHitStartTime = DateTime.Now;
            }
            else if ((DateTime.Now - _edgeHitStartTime).TotalMilliseconds >= SwitchDelayMs)
            {
                // Trigger!
                _entryVirtualY = y;
                OnEdgeHit?.Invoke(currentEdge);
                _lastDetectedEdge = ScreenEdge.None; // Reset for next time
            }
        }
        else
        {
            _lastDetectedEdge = ScreenEdge.None;
        }
    }

    // -----------------------------------------------------------------------
    // Mouse Buttons
    // -----------------------------------------------------------------------

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        if (!_isRemoteMode) return;
        e.SuppressEvent = true;
        _onData(PacketSerializer.SerializeMouseDown(ButtonIdFromSharpHook(e.Data.Button)));
    }

    private void OnMouseReleased(object? sender, MouseHookEventArgs e)
    {
        if (!_isRemoteMode) return;
        e.SuppressEvent = true;
        _onData(PacketSerializer.SerializeMouseUp(ButtonIdFromSharpHook(e.Data.Button)));
    }

    // -----------------------------------------------------------------------
    // Mouse Wheel
    // -----------------------------------------------------------------------

    private void OnMouseWheel(object? sender, MouseWheelHookEventArgs e)
    {
        if (!_isRemoteMode) return;
        e.SuppressEvent = true;

        // SharpHook Rotation: positive = up/forward, negative = down/backward
        // Input Leap convention: +120 per notch forward, -120 per notch backward
        short rotation = e.Data.Rotation;
        // SharpHook already reports in 120-unit increments on most platforms;
        // normalize to ±120 if value seems to be in raw ticks (usually ±1 or ±3)
        short yDelta = (short)(rotation < 0 ? -120 : 120);
        // No horizontal scroll data from SharpHook's basic wheel event → xDelta = 0
        _onData(PacketSerializer.SerializeMouseWheel(0, yDelta));
    }

    // -----------------------------------------------------------------------
    // Keyboard
    // -----------------------------------------------------------------------

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        // Always track modifiers, even in local mode
        UpdateModifiers(e.Data.KeyCode, true);

        if (!_isRemoteMode) return;
        e.SuppressEvent = true;

        if (!KeyMap.KeyCodeToId.TryGetValue(e.Data.KeyCode, out ushort keyId))
        {
            Console.WriteLine($"[INPUT] Unknown KeyCode: {e.Data.KeyCode} — skipping.");
            return;
        }

        ushort mods = KeyMap.GetModifierMask(_shiftDown, _ctrlDown, _altDown, _metaDown);
        _onData(PacketSerializer.SerializeKeyDown(keyId, mods));
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        // Always track modifiers, even in local mode
        UpdateModifiers(e.Data.KeyCode, false);

        if (!_isRemoteMode) return;
        e.SuppressEvent = true;

        if (!KeyMap.KeyCodeToId.TryGetValue(e.Data.KeyCode, out ushort keyId))
            return;

        ushort mods = KeyMap.GetModifierMask(_shiftDown, _ctrlDown, _altDown, _metaDown);
        _onData(PacketSerializer.SerializeKeyUp(keyId, mods));
    }

    private void UpdateModifiers(KeyCode code, bool pressed)
    {
        switch (code)
        {
            case KeyCode.VcLeftShift:
            case KeyCode.VcRightShift:   _shiftDown = pressed; break;
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl: _ctrlDown  = pressed; break;
            case KeyCode.VcLeftAlt:
            case KeyCode.VcRightAlt:     _altDown   = pressed; break;
            case KeyCode.VcLeftMeta:
            case KeyCode.VcRightMeta:    _metaDown  = pressed; break;
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private short GetStickyX() =>
        _activeEdge == ScreenEdge.Right  ? (short)(_screenWidth - 1) :
        _activeEdge == ScreenEdge.Left   ? (short)0 :
        (short)(_screenWidth / 2);

    /// <summary>Maps SharpHook MouseButton to a 1-indexed ButtonID (matches Input Leap convention).</summary>
    private static byte ButtonIdFromSharpHook(MouseButton btn) => btn switch
    {
        MouseButton.Button1 => 1, // Left
        MouseButton.Button2 => 2, // Right
        MouseButton.Button3 => 3, // Middle
        MouseButton.Button4 => 4,
        MouseButton.Button5 => 5,
        _                   => 1,
    };

    public double GetEntryVirtualY() => _entryVirtualY;
}
