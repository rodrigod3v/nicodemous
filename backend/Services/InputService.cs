using SharpHook;
using SharpHook.Native;
using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class InputService
{
    private readonly TaskPoolGlobalHook _hook;
    private readonly Action<string> _onEvent;

    public InputService(Action<string> onEvent)
    {
        _hook = new TaskPoolGlobalHook();
        _onEvent = onEvent;

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
        var data = new { type = "mouse_move", x = e.Data.X, y = e.Data.Y };
        _onEvent(JsonSerializer.Serialize(data));
    }

    private void OnMouseClicked(object? sender, MouseHookEventArgs e)
    {
        var data = new { type = "mouse_click", button = e.Data.Button.ToString() };
        _onEvent(JsonSerializer.Serialize(data));
    }

    private void OnKeyTyped(object? sender, KeyboardHookEventArgs e)
    {
        var data = new { type = "key_press", key = e.Data.KeyCode.ToString() };
        _onEvent(JsonSerializer.Serialize(data));
    }
}
