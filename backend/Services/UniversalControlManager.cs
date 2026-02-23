using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class UniversalControlManager
{
    private readonly InputService _inputService;
    private readonly NetworkService _networkService;
    private readonly InjectionService _injectionService;
    private readonly AudioService _audioService;
    private readonly AudioReceiveService _audioReceiveService;
    
    private bool _isRemoteControlActive = false;
    private int _screenWidth = 1920; 

    public UniversalControlManager()
    {
        _injectionService = new InjectionService();
        _networkService = new NetworkService(8888);
        _inputService = new InputService(HandleLocalInput);
        _audioService = new AudioService(HandleAudioCaptured);
        _audioReceiveService = new AudioReceiveService();

        _networkService.StartListening(HandleRemoteMessage);
    }

    public void Start()
    {
        _inputService.Start();
    }

    public void Stop()
    {
        _inputService.Stop();
        _audioService.StopCapture();
        _audioReceiveService.Stop();
        _networkService.Stop();
    }

    public void ConnectTo(string ip)
    {
        _networkService.SetTarget(ip, 8888);
    }

    public void ToggleService(string name, bool enabled)
    {
        switch (name)
        {
            case "input":
                if (enabled) _inputService.Start();
                else _inputService.Stop();
                break;
            case "audio":
                if (enabled) _audioService.StartCapture();
                else _audioService.StopCapture();
                break;
        }
    }

    private void HandleLocalInput(string json)
    {
        if (!_isRemoteControlActive) return;
        _networkService.Send(json);
    }

    private void HandleAudioCaptured(byte[] data)
    {
        // For simplicity, we wrap audio in a JSON-like structure or send directly
        // In a real app, we'd use a more efficient binary protocol
        var message = new { type = "audio_frame", data = Convert.ToBase64String(data) };
        _networkService.Send(JsonSerializer.Serialize(message));
    }

    private void HandleRemoteMessage(string json)
    {
        try 
        {
            var doc = JsonDocument.Parse(json);
            string? type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "mouse_move":
                case "mouse_click":
                case "key_press":
                    _injectionService.Inject(json);
                    break;
                case "audio_frame":
                    string base64 = doc.RootElement.GetProperty("data").GetString() ?? "";
                    byte[] audioData = Convert.FromBase64String(base64);
                    _audioReceiveService.ProcessFrame(audioData);
                    break;
            }
        }
        catch { /* Ignore malformed packets */ }
    }

    public void SetRemoteControlState(bool active)
    {
        _isRemoteControlActive = active;
        if (active) Console.WriteLine("Remote Control Mode: ACTIVE");
        else Console.WriteLine("Remote Control Mode: LOCAL");
    }
}
