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

    public event Action<ScreenEdge>? OnEdgeHit;

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
        // Simple lock: if in remote mode, keep mouse at the edge it crossed
        if (_activeEdge == ScreenEdge.Right && x < _screenWidth - 100)
        {
            _simulator.SimulateMouseMovement(_screenWidth, y);
        }
        else if (_activeEdge == ScreenEdge.Left && x > 100)
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
