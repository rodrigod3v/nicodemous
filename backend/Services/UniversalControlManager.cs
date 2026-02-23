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

    public UniversalControlManager()
    {
        _injectionService = new InjectionService();
        _networkService = new NetworkService(8888);
        _inputService = new InputService(HandleLocalData);
        _audioService = new AudioService(HandleAudioCaptured);
        _audioReceiveService = new AudioReceiveService();

        _networkService.StartListening(HandleRemoteData);
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

    private void HandleLocalData(byte[] data)
    {
        if (!_isRemoteControlActive) return;
        _networkService.Send(data);
    }

    private void HandleAudioCaptured(byte[] data)
    {
        _networkService.Send(PacketSerializer.SerializeAudioFrame(data));
    }

    private void HandleRemoteData(byte[] data)
    {
        var (type, payload) = PacketSerializer.Deserialize(data);

        switch (type)
        {
            case PacketType.MouseMove:
                dynamic moveData = payload;
                _injectionService.InjectMouseMove(moveData.x, moveData.y);
                break;
            case PacketType.MouseClick:
                dynamic clickData = payload;
                _injectionService.InjectMouseClick(clickData.button);
                break;
            case PacketType.KeyPress:
                dynamic keyData = payload;
                _injectionService.InjectKeyPress(keyData.key);
                break;
            case PacketType.AudioFrame:
                _audioReceiveService.ProcessFrame((byte[])payload);
                break;
        }
    }

    public void SetRemoteControlState(bool active)
    {
        _isRemoteControlActive = active;
        if (active) Console.WriteLine("Remote Control Mode: ACTIVE");
        else Console.WriteLine("Remote Control Mode: LOCAL");
    }
}
