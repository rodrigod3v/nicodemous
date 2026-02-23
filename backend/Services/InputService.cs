using SharpHook;
using SharpHook.Native;
using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class InputService
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly Action<byte[]> _onData;

    public InputService(Action<byte[]> onData)
    {
        _hook = new TaskPoolGlobalHook();
        _onData = onData;

        _hook.MouseMoved += OnMouseMoved;
        _hook.MouseClicked += OnMouseClicked;
        _hook.KeyTyped += OnKeyTyped;
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
        _onData(PacketSerializer.SerializeMouseMove(e.Data.X, e.Data.Y));
    }

    private void OnMouseClicked(object? sender, MouseHookEventArgs e)
    {
        _onData(PacketSerializer.SerializeMouseClick(e.Data.Button.ToString()));
    }

    private void OnKeyTyped(object? sender, KeyboardHookEventArgs e)
    {
        _onData(PacketSerializer.SerializeKeyPress(e.Data.KeyCode.ToString()));
    }
}
