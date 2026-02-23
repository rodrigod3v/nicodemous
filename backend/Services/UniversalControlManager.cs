using System.Text.Json;
using Photino.NET;

namespace Nicodemous.Backend.Services;

public class UniversalControlManager
{
    private readonly InputService _inputService;
    private readonly NetworkService _networkService;
    private readonly InjectionService _injectionService;
    private readonly AudioService _audioService;
    private readonly AudioReceiveService _audioReceiveService;
    private readonly DiscoveryService _discoveryService;
    
    private bool _isRemoteControlActive = false;

    public string PairingCode => _discoveryService.PairingCode;

    public UniversalControlManager()
    {
        _injectionService = new InjectionService();
        _networkService = new NetworkService(8888);
        _inputService = new InputService(HandleLocalData);
        _audioService = new AudioService(HandleAudioCaptured);
        _audioReceiveService = new AudioReceiveService();
        _discoveryService = new DiscoveryService(Environment.MachineName);

        _networkService.StartListening(HandleRemoteData);
        _inputService.OnEdgeHit += HandleEdgeHit;

        // Detect Primary Screen Size (Windows focused)
#if WINDOWS
        try 
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (screen != null)
            {
                _inputService.SetScreenSize((short)screen.Bounds.Width, (short)screen.Bounds.Height);
            }
        }
        catch { /* Fallback to 1920x1080 */ }
#endif
    }

    private void HandleEdgeHit(ScreenEdge edge)
    {
        // When edge is hit, we AUTOMATICALLY enter remote mode if a device is connected
        if (!_isRemoteControlActive)
        {
            SetRemoteControlState(true);
            // Optionally send a "Focus" packet to remote
        }
    }

    public void Start()
    {
        _inputService.Start();
        _discoveryService.Start();
    }

    public void Stop()
    {
        _inputService.Stop();
        _audioService.StopCapture();
        _audioReceiveService.Stop();
        _networkService.Stop();
        _discoveryService.Stop();
    }

    public void ConnectByCode(string code, PhotinoWindow? window = null)
    {
        var device = _discoveryService.GetDiscoveredDevices().FirstOrDefault(d => d.Code == code);
        if (device != null)
        {
            _networkService.SetTarget(device.IPAddress, 8888);
            if (window != null)
            {
                window.SendWebMessage(JsonSerializer.Serialize(new { type = "connection_status", status = "Connected" }));
            }
            Console.WriteLine($"Connected to {device.Name} ({device.IPAddress}) via code {code}");
        }
    }

    public void ConnectTo(string destination)
    {
        // Check if destination is a Pairing Code or IP
        string? ip = _discoveryService.GetIpByCode(destination);
        if (ip != null)
        {
            Console.WriteLine($"Resolved Pairing Code {destination} to {ip}");
            _networkService.SetTarget(ip, 8888);
        }
        else
        {
            // Assume it's an IP
            _networkService.SetTarget(destination, 8888);
        }
    }

    public List<DiscoveredDevice> GetDevices() => _discoveryService.GetDiscoveredDevices();

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
        _inputService.SetRemoteMode(active);
        
        if (active) Console.WriteLine("Remote Control Mode: ACTIVE (Input Redirection On)");
        else Console.WriteLine("Remote Control Mode: LOCAL");
    }
}
