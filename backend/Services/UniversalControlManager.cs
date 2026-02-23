using System.Text.Json;
using System.Linq;
using Photino.NET;
using System.Runtime.InteropServices;

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
    private PhotinoWindow? _window;

    public string PairingCode => _discoveryService.PairingCode;

    public void SetWindow(PhotinoWindow window)
    {
        _window = window;
        _discoveryService.OnDeviceDiscovered += (devices) => {
            _window.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_result", devices }));
        };
    }

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
        _inputService.OnReturn += () => SetRemoteControlState(false);

        // Detect Primary Screen Size based on OS
        DetectScreenSize();
    }

    private void DetectScreenSize()
    {
        // Default fallback
        short width = 1920;
        short height = 1080;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
#if WINDOWS
            try 
            {
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                if (screen != null)
                {
                    width = (short)screen.Bounds.Width;
                    height = (short)screen.Bounds.Height;
                }
            }
            catch { }
#endif
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
        {
            // On macOS, SharpHook or Photino could provide this, but for now we'll use a standard fallback or detect via Photino if possible.
            // Photino doesn't expose Screen directly, so we use a common Mac resolution or ideally SharpHook's hook can tell us.
            // For now, let's stick with 1920x1080 as default on Mac unless we implement a native P/Invoke.
            width = 1440; // Common Retina base
            height = 900;
        }

        _inputService.SetScreenSize(width, height);
        _injectionService.SetScreenSize(width, height);
        Console.WriteLine($"System Resolution Detected: {width}x{height}");
    }

    private void HandleEdgeHit(ScreenEdge edge)
    {
        // When edge is hit, we AUTOMATICALLY enter remote mode IF a target is set
        if (!_isRemoteControlActive && _networkService.HasTarget)
        {
            SetRemoteControlState(true);
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

    public void Connect(string target, PhotinoWindow? window = null)
    {
        string? ip = null;

        // Try to resolve as Pairing Code first
        var device = _discoveryService.GetDiscoveredDevices().FirstOrDefault(d => d.Code.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (device != null)
        {
            ip = device.IPAddress;
            Console.WriteLine($"Resolved Pairing Code {target} to {ip} ({device.Name})");
        }
        else if (System.Net.IPAddress.TryParse(target, out _))
        {
            // It's a valid IP address
            ip = target;
        }

        if (ip != null)
        {
            _networkService.SetTarget(ip, 8888);
            if (window != null)
            {
                window.SendWebMessage(JsonSerializer.Serialize(new { type = "connection_status", status = "Connected" }));
            }
            Console.WriteLine($"Connection target set to {ip}");
        }
        else
        {
            Console.WriteLine($"Unrecognized target: {target}. Not a discovered code nor a valid IP.");
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

    public List<DiscoveredDevice> GetDevices() 
    {
        _discoveryService.ClearDiscoveredDevices();
        // Since broadcaster/listener are always running, clearing will allow fresh items to populate
        return _discoveryService.GetDiscoveredDevices();
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

    public void UpdateSettings(string edge, bool lockInput)
    {
        _inputService.SetActiveEdge(edge);
        _inputService.SetInputLock(lockInput);
    }

    public void SetRemoteControlState(bool active)
    {
        _isRemoteControlActive = active;
        _inputService.SetRemoteMode(active);
        
        if (active) Console.WriteLine("Remote Control Mode: ACTIVE (Input Redirection On)");
        else Console.WriteLine("Remote Control Mode: LOCAL");
    }
}
