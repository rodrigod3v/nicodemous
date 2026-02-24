using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using Photino.NET;

namespace Nicodemous.Backend.Services;

public class UniversalControlManager : IDisposable
{
    private readonly InputService        _inputService;
    private readonly NetworkService      _networkService;
    private readonly InjectionService    _injectionService;
    private readonly ClipboardService    _clipboardService;
    private readonly AudioService        _audioService;
    private readonly AudioReceiveService _audioReceiveService;
    private readonly DiscoveryService    _discoveryService;
    private readonly SettingsService     _settingsService;

    private bool _isRemoteControlActive = false;
    private PhotinoWindow? _window;
    private bool _disposed;

    public string PairingCode => _settingsService.GetSettings().PairingCode;

    // -----------------------------------------------------------------------
    // Construction
    // -----------------------------------------------------------------------

    public UniversalControlManager(SettingsService settings)
    {
        _settingsService     = settings;
        _clipboardService    = new ClipboardService();
        _injectionService    = new InjectionService(_clipboardService);
        _networkService      = new NetworkService(8890);
        _inputService        = new InputService(SendLocalData);
        _audioService        = new AudioService(HandleAudioCaptured);
        _audioReceiveService = new AudioReceiveService();
        _discoveryService    = new DiscoveryService(Environment.MachineName);

        // Apply initial settings
        ApplySettings();

        // Network
        _networkService.StartListening(HandleRemoteData);
        _networkService.OnConnected += () =>
        {
            // TCP ready — send handshake with local PairingCode
            _networkService.Send(PacketSerializer.SerializeHandshake(Environment.MachineName, PairingCode));
            Console.WriteLine($"[MANAGER] Handshake sent (PIN: {PairingCode}) after TCP connection established.");
            SendUiMessage("connection_status", "Handshaking...");

            // Start syncing our local clipboard to the peer immediately
            _clipboardService.StartMonitoring(text =>
                _networkService.Send(PacketSerializer.SerializeClipboardPush(text)));

            // Also request the peer's current clipboard content
            _networkService.Send(PacketSerializer.SerializeClipboardPull());
        };
        _networkService.OnDisconnected += () =>
        {
            Console.WriteLine("[MANAGER] Remote disconnected.");
            _clipboardService.StopMonitoring();
            if (_isRemoteControlActive) SetRemoteControlState(false);
            SendUiMessage("connection_status", "Disconnected");
        };

        // Input
        _inputService.OnEdgeHit += HandleEdgeHit;
        _inputService.OnReturn  += () => SetRemoteControlState(false);

        DetectAndSetScreenSize();
    }

    public void SetWindow(PhotinoWindow window)
    {
        _window = window;
        
        // Connect the clipboard service to the UI thread for macOS stability
        _clipboardService.InvokeOnMainThread = (action) => window.Invoke(action);

        _discoveryService.OnDeviceDiscovered += devices =>
            window.Invoke(() =>
                window.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_result", devices }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    // -----------------------------------------------------------------------
    // Remote Control State
    // -----------------------------------------------------------------------

    public void SetRemoteControlState(bool active)
    {
        _isRemoteControlActive = active;
        _inputService.SetRemoteMode(active);
        Console.WriteLine(active ? "[MANAGER] Remote Control: ACTIVE" : "[MANAGER] Remote Control: LOCAL");
    }

    private void HandleEdgeHit(ScreenEdge edge)
    {
        if (!_isRemoteControlActive && _networkService.IsConnected)
        {
            SetRemoteControlState(true);
        }
        else if (!_networkService.IsConnected)
        {
            Console.WriteLine("[MANAGER] Edge hit but no active connection — ignored.");
        }
    }

    // -----------------------------------------------------------------------
    // Connection
    // -----------------------------------------------------------------------

    public void Connect(string target, PhotinoWindow? window = null)
    {
        string? ip = ResolveTarget(target);
        if (ip == null)
        {
            string msg = $"Cannot resolve '{target}' — not a discovered code or valid IP.";
            Console.WriteLine($"[MANAGER] {msg}");
            (window ?? _window)?.Invoke(() =>
                (window ?? _window)!.SendWebMessage(JsonSerializer.Serialize(new { type = "connection_status", status = "Error: Invalid IP" }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
            return;
        }

        _networkService.SetTarget(ip, 8890);
        // Handshake is sent inside OnConnected (after TCP is actually ready)

        Console.WriteLine($"[MANAGER] Connecting to {ip}...");
        (window ?? _window)?.Invoke(() =>
            (window ?? _window)!.SendWebMessage(JsonSerializer.Serialize(new { type = "connection_status", status = "Connecting..." }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }

    private string? ResolveTarget(string target)
    {
        var device = _discoveryService.GetDiscoveredDevices()
                                       .FirstOrDefault(d => d.Code.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (device != null) return device.Ip;
        if (System.Net.IPAddress.TryParse(target, out _)) return target;
        return null;
    }

    public List<DiscoveredDevice> GetDevices()
    {
        _discoveryService.BroadcastNow();
        return _discoveryService.GetDiscoveredDevices();
    }

    public void ToggleService(string name, bool enabled)
    {
        switch (name)
        {
            case "input":
                if (enabled) _inputService.Start(); else _inputService.Stop();
                break;
            case "audio":
                if (enabled) _audioService.StartCapture(); else _audioService.StopCapture();
                break;
        }
    }

    public string GetSettingsJson()
    {
        return JsonSerializer.Serialize(_settingsService.GetSettings());
    }

    public void UpdateSettings(string edge, bool lockInput, int delay, int cornerSize, double sensitivity = 1.0)
    {
        _inputService.SetActiveEdge(edge);
        _inputService.SetInputLock(lockInput);
        _inputService.SwitchDelayMs = delay;
        _inputService.CornerSize = cornerSize;

        // Persist
        var s = _settingsService.GetSettings();
        s.ActiveEdge = edge;
        s.SwitchingDelayMs = delay;
        s.DeadCornerSize = cornerSize;
        s.MouseSensitivity = sensitivity;
        s.LockInput = lockInput;
        _settingsService.Save();
    }

    private void ApplySettings()
    {
        var s = _settingsService.GetSettings();
        _inputService.SwitchDelayMs = s.SwitchingDelayMs;
        _inputService.CornerSize = s.DeadCornerSize;
        _inputService.MouseSensitivity = s.MouseSensitivity;
        _inputService.SetInputLock(s.LockInput);

        if (System.Enum.TryParse<ScreenEdge>(s.ActiveEdge, out var edge))
            _inputService.SetActiveEdge(s.ActiveEdge);
    }

    // -----------------------------------------------------------------------
    // Data Routing — Local → Remote
    // -----------------------------------------------------------------------

    private void SendLocalData(byte[] framedPacket)
    {
        if (!_isRemoteControlActive) return;
        _networkService.Send(framedPacket);
    }

    private void HandleAudioCaptured(byte[] data)
    {
        _networkService.Send(PacketSerializer.SerializeAudioFrame(data));
    }

    // -----------------------------------------------------------------------
    // Data Routing — Remote → Injection
    // -----------------------------------------------------------------------

    private void HandleRemoteData(byte[] buffer)
    {
        try
        {
            var (type, payload) = PacketSerializer.Deserialize(buffer);

            switch (type)
            {
                case PacketType.MouseRelMove:
                    var rel = (MouseRelMoveData)payload;
                    _injectionService.InjectMouseRelMove(rel.Dx, rel.Dy);
                    break;

                case PacketType.MouseMove:
                    var abs = (MouseMoveData)payload;
                    _injectionService.InjectMouseMove(abs.X, abs.Y);
                    break;

                case PacketType.MouseDown:
                    var down = (MouseButtonData)payload;
                    _injectionService.InjectMouseDown(down.ButtonId);
                    break;

                case PacketType.MouseUp:
                    var up = (MouseButtonData)payload;
                    _injectionService.InjectMouseUp(up.ButtonId);
                    break;

                case PacketType.MouseWheel:
                    var wheel = (MouseWheelData)payload;
                    _injectionService.InjectMouseWheel(wheel.XDelta, wheel.YDelta);
                    break;

                case PacketType.KeyDown:
                    var kd = (KeyEventData)payload;
                    _injectionService.InjectKeyDown(kd.KeyId, kd.Modifiers);
                    break;

                case PacketType.KeyUp:
                    var ku = (KeyEventData)payload;
                    _injectionService.InjectKeyUp(ku.KeyId, ku.Modifiers);
                    break;

                case PacketType.AudioFrame:
                    var audio = (AudioFrameData)payload;
                    _audioReceiveService.ProcessFrame(audio.Data);
                    break;

                case PacketType.Handshake:
                    var hs = (HandshakeData)payload;
                    
                    // Verify pairing code
                    if (hs.PairingCode != PairingCode)
                    {
                        Console.WriteLine($"[MANAGER] ACCESS DENIED: Handshake from '{hs.MachineName}' used wrong PIN '{hs.PairingCode}'. local PIN is '{PairingCode}'.");
                        _networkService.Send(PacketSerializer.SerializeClipboardPush("ERROR: Invalid Pairing Code. Connection Rejected."));
                        _networkService.Stop();
                        SendUiMessage("connection_status", "Rejeitado: PIN Inválido");
                        break;
                    }

                    Console.WriteLine($"[MANAGER] Handshake from '{hs.MachineName}' (PIN Verified). Sending ACK.");
                    _networkService.Send(PacketSerializer.SerializeHandshakeAck());
                    SendUiMessage("connection_status", $"Controlled by {hs.MachineName}");
                    // Also start monitoring our clipboard so we push changes back to the controller
                    _clipboardService.StartMonitoring(text =>
                        _networkService.Send(PacketSerializer.SerializeClipboardPush(text)));
                    
                    // Request the controller's current clipboard too
                    _networkService.Send(PacketSerializer.SerializeClipboardPull());
                    break;

                case PacketType.ClipboardPush:
                    // Peer sent us their clipboard — apply it locally
                    // Ctrl+V now works natively since both machines share the same clipboard
                    var cbPush = (ClipboardData)payload;
                    Console.WriteLine($"[MANAGER] ClipboardPush received ({cbPush.Text.Length} chars).");
                    _clipboardService.SetText(cbPush.Text);
                    break;

                case PacketType.HandshakeAck:
                    Console.WriteLine("[MANAGER] Handshake ACK — connection verified.");
                    SendUiMessage("connection_status", "Connected ✓");
                    break;

                case PacketType.ClipboardPull:
                    // Peer wants our current clipboard content
                    string currentText = _clipboardService.GetText();
                    if (!string.IsNullOrEmpty(currentText))
                    {
                        Console.WriteLine($"[MANAGER] ClipboardPull received. Sending current content ({currentText.Length} chars).");
                        _networkService.Send(PacketSerializer.SerializeClipboardPush(currentText));
                    }
                    else
                    {
                        Console.WriteLine("[MANAGER] ClipboardPull received, but local clipboard is empty.");
                    }
                    break;

                case PacketType.Ping:
                    _networkService.Send(PacketSerializer.SerializePing()); // Pong
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MANAGER] HandleRemoteData error: {ex.Message}");
        }
    }

    // -----------------------------------------------------------------------
    // Screen Size Detection
    // -----------------------------------------------------------------------

    private void DetectAndSetScreenSize()
    {
        short width = 1920, height = 1080;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            (width, height) = DetectScreenWindows();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            (width, height) = DetectScreenMacOS();

        _inputService.SetScreenSize(width, height);
        _injectionService.SetScreenSize(width, height);
        Console.WriteLine($"[MANAGER] Screen size: {width}x{height}");
    }

    private static (short w, short h) DetectScreenWindows()
    {
#if WINDOWS
        try
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (screen != null)
                return ((short)screen.Bounds.Width, (short)screen.Bounds.Height);
        }
        catch (Exception ex) { Console.WriteLine($"[MANAGER] Windows screen detect failed: {ex.Message}"); }
#endif
        return (1920, 1080);
    }

    /// <summary>
    /// Uses CoreGraphics CGDisplayBounds to get the LOGICAL (point) resolution,
    /// which is what the OS uses for mouse coordinates and is correct for Retina displays.
    /// (system_profiler reports physical pixels — e.g. 2560 — which would be wrong for a
    ///  1280-point Retina display and cause the cursor to land at double the expected position.)
    /// </summary>
    private static (short w, short h) DetectScreenMacOS()
    {
        try
        {
            uint displayId = CGMainDisplayID();
            var bounds = CGDisplayBounds(displayId);
            Console.WriteLine($"[MANAGER] macOS CGDisplayBounds: {bounds.Width}x{bounds.Height} @ ({bounds.X},{bounds.Y})");
            return ((short)bounds.Width, (short)bounds.Height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MANAGER] macOS CoreGraphics screen detect failed: {ex.Message}. Falling back to 1440x900.");
            return (1440, 900);
        }
    }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern uint CGMainDisplayID();

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect { public double X; public double Y; public double Width; public double Height; }

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern CGRect CGDisplayBounds(uint display);

    // -----------------------------------------------------------------------
    // UI Messaging
    // -----------------------------------------------------------------------

    private void SendUiMessage(string type, string value)
    {
        _window?.Invoke(() =>
            _window.SendWebMessage(JsonSerializer.Serialize(new { type, status = value }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }
}
