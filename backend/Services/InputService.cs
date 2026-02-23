using SharpHook;
using SharpHook.Native;
using System.Text.Json;

namespace Nicodemous.Backend.Services;

public enum ScreenEdge
{
    None,
    Left,
    Right,
    Top,
    Bottom
}

public class InputService
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly IEventSimulator _simulator;
    private readonly Action<byte[]> _onData;
    private bool _isRemoteMode = false;
    private short _screenWidth = 1920;
    private short _screenHeight = 1080;
    private ScreenEdge _activeEdge = ScreenEdge.Right; // Default: Right edge crosses to remote
    private bool _isInputLocked = true; // Default: Lock mouse to edge when in remote mode
    private DateTime _lastReturnTime = DateTime.MinValue;
    private const int CooldownMs = 1000;

    public event Action<ScreenEdge>? OnEdgeHit;
    public event Action? OnReturn;

    public void SetActiveEdge(string edge)
    {
        _activeEdge = Enum.TryParse<ScreenEdge>(edge, true, out var result) ? result : ScreenEdge.Right;
        Console.WriteLine($"InputService: Active Edge set to {_activeEdge}");
    }

    public void SetInputLock(bool locked)
    {
        _isInputLocked = locked;
        Console.WriteLine($"InputService: Input Lock set to {locked}");
    }

    public InputService(Action<byte[]> onData)
    {
        _hook = new TaskPoolGlobalHook();
        _simulator = new EventSimulator();
        _onData = onData;

        _hook.MouseMoved += OnMouseMoved;
        _hook.MouseClicked += OnMouseClicked;
        _hook.KeyTyped += OnKeyTyped;
    }

    public void SetScreenSize(short width, short height)
    {
        _screenWidth = width;
        _screenHeight = height;
    }

    public void SetRemoteMode(bool enabled)
    {
        _isRemoteMode = enabled;
    }

    public void Start()
    {
        Task.Run(() => _hook.Run());
    }

    public void Stop()
    {
        _hook.Dispose();
    }

    private void OnMouseMoved(object? sender, MouseHookEventArgs e)
    {
        if (_isRemoteMode)
        {
            if (_screenWidth == 0 || _screenHeight == 0) return;

            // Normalize current position to 0-65535 range
            ushort normX = (ushort)((double)e.Data.X / _screenWidth * 65535);
            ushort normY = (ushort)((double)e.Data.Y / _screenHeight * 65535);

            // Send normalized coordinates to remote
            _onData(PacketSerializer.SerializeMouseMove(normX, normY));

            // Lock mouse to center or edge to prevent it from interacting locally
            HandleMouseLock(e.Data.X, e.Data.Y);
        }
        else
        {
            // Check for cooldown to avoid immediate re-entry when pulling back
            if ((DateTime.Now - _lastReturnTime).TotalMilliseconds < CooldownMs) return;

            // Check for edge hit to trigger remote mode
            if (e.Data.X >= _screenWidth - 1 && _activeEdge == ScreenEdge.Right)
            {
                OnEdgeHit?.Invoke(ScreenEdge.Right);
            }
            else if (e.Data.X <= 0 && _activeEdge == ScreenEdge.Left)
            {
                OnEdgeHit?.Invoke(ScreenEdge.Left);
            }
        }
    }

    private void HandleMouseLock(short x, short y)
    {
        // Return detection: if user pulls mouse far enough away from the edge, we EXIT remote mode
        const int returnThreshold = 200; 

        if (_activeEdge == ScreenEdge.Right && x < _screenWidth - returnThreshold)
        {
            _lastReturnTime = DateTime.Now;
            OnReturn?.Invoke();
            return;
        }
        else if (_activeEdge == ScreenEdge.Left && x > returnThreshold)
        {
            _lastReturnTime = DateTime.Now;
            OnReturn?.Invoke();
            return;
        }

        if (!_isInputLocked) return;

        // Simple lock: if in remote mode, keep mouse at the edge it crossed
        if (_activeEdge == ScreenEdge.Right && x < _screenWidth - 5)
        {
            _simulator.SimulateMouseMovement(_screenWidth, y);
        }
        else if (_activeEdge == ScreenEdge.Left && x > 5)
        {
            _simulator.SimulateMouseMovement(0, y);
        }
    }

    private void OnMouseClicked(object? sender, MouseHookEventArgs e)
    {
        if (_isRemoteMode)
        {
            _onData(PacketSerializer.SerializeMouseClick(e.Data.Button.ToString()));
        }
    }

    private void OnKeyTyped(object? sender, KeyboardHookEventArgs e)
    {
        if (_isRemoteMode)
        {
            _onData(PacketSerializer.SerializeKeyPress(e.Data.KeyCode.ToString()));
        }
    }
}
