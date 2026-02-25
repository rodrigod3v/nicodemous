using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Collections.Concurrent;
using Photino.NET;

namespace nicodemouse.Backend.Services;

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
    private string? _targetPairingCode;
    private bool _disposed;
    private DateTime _lastSystemInfoTime = DateTime.MinValue;
    private bool _isWindowReady = false;
    private readonly ConcurrentQueue<Action> _messageQueue = new();
    
    public event Action<bool>? OnConnectionChanged;
    public event Action<bool>? OnRemoteControlChanged;

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
        _discoveryService.SignalingServerUrl = _settingsService.GetSettings().SignalingServerUrl;

        // Apply initial settings
        ApplySettings();

        // Network
        _networkService.StartListening(HandleRemoteData);
        _networkService.OnConnected += (isIncoming) =>
        {
            if (!isIncoming)
            {
                // We are the initiator (Controller)
                // Use the target PIN we captured during Connect()
                string pinToSend = _targetPairingCode ?? PairingCode;
                _networkService.Send(PacketSerializer.SerializeHandshake(Environment.MachineName, pinToSend));
                Console.WriteLine($"[MANAGER] Handshake sent (PIN: {pinToSend}) as Controller.");
                SendUiMessage("connection_status", "Handshaking...");
            }
            else
            {
                // We are the receiver (Client/Controlled)
                Console.WriteLine("[MANAGER] Incoming connection — waiting for handshake...");
            }

            OnConnectionChanged?.Invoke(true);

            // Both sides can start monitoring clipboard (stays bidirectional)
            _clipboardService.StartMonitoring(text =>
                _networkService.Send(PacketSerializer.SerializeClipboardPush(text)));

            // If we are the controller, request initial content
            if (!isIncoming)
                _networkService.Send(PacketSerializer.SerializeClipboardPull());
        };
        _networkService.OnDisconnected += () =>
        {
            Console.WriteLine("[MANAGER] Remote disconnected.");
            _clipboardService.StopMonitoring();
            if (_isRemoteControlActive) SetRemoteControlState(false);
            OnConnectionChanged?.Invoke(false);
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
        _clipboardService.InvokeOnMainThread = (action) => 
        {
            if (_window != null && _isWindowReady) _window.Invoke(action);
            else _messageQueue.Enqueue(() => _window?.Invoke(action));
        };

        _discoveryService.OnDeviceDiscovered += devices =>
        {
            QueueOrSend(() =>
                _window?.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_result", devices }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
        };
    }

    public void NotifyWindowReady()
    {
        if (_isWindowReady) return;
        _isWindowReady = true;

        Console.WriteLine("[MANAGER] Window signal: READY. Flushing message queue...");
        while (_messageQueue.TryDequeue(out var action))
        {
            try { _window?.Invoke(action); } catch { }
        }
    }

    private void QueueOrSend(Action action)
    {
        if (_window != null && _isWindowReady)
        {
            _window.Invoke(action);
        }
        else
        {
            _messageQueue.Enqueue(action);
        }
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    public void Start()
    {
        _inputService.Start();
        _discoveryService.Start();
    }

    public void RefreshDiscovery()
    {
        _discoveryService.BroadcastNow();
        _discoveryService.TriggerRemoteFetch();
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
        OnRemoteControlChanged?.Invoke(active);
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
            QueueOrSend(() =>
                (window ?? _window)!.SendWebMessage(JsonSerializer.Serialize(new { type = "connection_status", status = "Error: Invalid IP" }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
            return;
        }

        // Capture the PIN for the handshake (it's either the target itself or the code from discovery)
        _targetPairingCode = ResolvePin(target);

        _networkService.SetTarget(ip, 8890);
        // Handshake is sent inside OnConnected (after TCP is actually ready)

        Console.WriteLine($"[MANAGER] Connecting to {ip}...");
        QueueOrSend(() =>
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

    private string? ResolvePin(string target)
    {
        // 1. Try to find device by Code
        var device = _discoveryService.GetDiscoveredDevices()
                                       .FirstOrDefault(d => d.Code.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (device != null) return device.Code;

        // 2. Try to find device by IP (if target is an IP)
        var deviceByIp = _discoveryService.GetDiscoveredDevices()
                                         .FirstOrDefault(d => d.Ip.Equals(target, StringComparison.OrdinalIgnoreCase));
        if (deviceByIp != null) return deviceByIp.Code;

        // 3. If target looks like a PIN itself
        if (target.Length == 6) return target;
        
        return null;
    }

    public List<DiscoveredDevice> GetDevices()
    {
        _discoveryService.BroadcastNow();
        return _discoveryService.GetDiscoveredDevices();
    }

    public void ToggleService(string name, bool enabled)
    {
        var s = _settingsService.GetSettings();
        Console.WriteLine($"[MANAGER] ToggleService called: name='{name}', enabled={enabled}");
        switch (name.Trim().ToLowerInvariant())
        {
            case "input":
                if (enabled) _inputService.Start(); else _inputService.Stop();
                s.EnableInput = enabled;
                break;
            case "clipboard":
                if (enabled) _clipboardService.StartMonitoring(t => _networkService.Send(PacketSerializer.SerializeClipboardPush(t)));
                else _clipboardService.StopMonitoring();
                s.EnableClipboard = enabled;
                break;
            case "audio":
                if (enabled) _audioService.StartCapture(); else _audioService.StopCapture();
                s.EnableAudio = enabled;
                break;
            case "disconnect":
                Console.WriteLine("[MANAGER] Disconnection requested by UI. Calling NetworkService.Disconnect()");
                _networkService.Disconnect();
                break;
            default:
                Console.WriteLine($"[MANAGER] Warning: ToggleService received unknown service name: '{name}'");
                break;
        }
        _settingsService.Save();
        SendSettingsToWeb();
    }

    public string GetSettingsJson()
    {
        // Only refresh system info (expensive monitor scan) if it hasn't been done in the last 2 minutes
        if ((DateTime.Now - _lastSystemInfoTime).TotalMinutes > 2)
        {
            SendSystemInfo();
        }
        return JsonSerializer.Serialize(_settingsService.GetSettings());
    }

    private void SendSystemInfo()
    {
        _lastSystemInfoTime = DateTime.Now;
        var monitors = new List<object>();
#if WINDOWS
        try {
            foreach (var screen in System.Windows.Forms.Screen.AllScreens) {
                monitors.Add(new { name = screen.DeviceName, isPrimary = screen.Primary });
            }
        } catch {}
#endif
        if (monitors.Count == 0) monitors.Add(new { name = "Default Monitor", isPrimary = true });

        QueueOrSend(() =>
            _window!.SendWebMessage(JsonSerializer.Serialize(new { 
                type = "system_info", 
                machineName = Environment.MachineName,
                monitors = monitors
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }

    public void UpdateSettings(string edge, bool lockInput, int delay, int cornerSize, double sensitivity = 1.0, int gestureThreshold = 1500, string? pairingCode = null, string? activeMonitor = null)
    {
        _inputService.SetActiveEdge(edge);
        _inputService.SetInputLock(lockInput);
        _inputService.SwitchDelayMs = delay;
        _inputService.CornerSize = cornerSize;
        _inputService.MouseSensitivity = sensitivity;
        _inputService.ReturnThreshold = gestureThreshold;

        // Persist
        var s = _settingsService.GetSettings();
        s.ActiveEdge = edge;
        s.SwitchingDelayMs = delay;
        s.DeadCornerSize = cornerSize;
        s.MouseSensitivity = sensitivity;
        s.GestureThreshold = gestureThreshold;
        s.LockInput = lockInput;
        if (!string.IsNullOrEmpty(activeMonitor)) s.ActiveMonitor = activeMonitor;

        if (!string.IsNullOrEmpty(pairingCode) && pairingCode.Length == 6)
        {
            s.PairingCode = pairingCode;
            _discoveryService.UpdatePairingCode(pairingCode);
            SendLocalIpToWeb(); // Notify UI of PIN change
        }

        _settingsService.Save();
        SendSettingsToWeb();
    }

    public void ResetSettings()
    {
        Console.WriteLine("[MANAGER] Resetting settings to defaults...");
        var defaultSettings = new AppSettings();
        _settingsService.UpdateSettings(defaultSettings);
        ApplySettings();
        SendSettingsToWeb();
    }

    private void ApplySettings()
    {
        var s = _settingsService.GetSettings();
        _inputService.SwitchDelayMs = s.SwitchingDelayMs;
        _inputService.CornerSize = s.DeadCornerSize;
        _inputService.MouseSensitivity = s.MouseSensitivity;
        _inputService.ReturnThreshold = s.GestureThreshold;
        _inputService.SetInputLock(s.LockInput);

        if (System.Enum.TryParse<ScreenEdge>(s.ActiveEdge, out var edge))
            _inputService.SetActiveEdge(s.ActiveEdge);

        // Apply service states without pushing back to UI to avoid loops
        if (s.EnableInput) _inputService.Start(); else _inputService.Stop();
        
        if (s.EnableClipboard) _clipboardService.StartMonitoring(t => _networkService.Send(PacketSerializer.SerializeClipboardPush(t)));
        else _clipboardService.StopMonitoring();

        if (s.EnableAudio) _audioService.StartCapture(); else _audioService.StopCapture();

        // Update discovery PIN
        _discoveryService.UpdatePairingCode(s.PairingCode);
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
                        _networkService.Disconnect();
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

                case PacketType.Disconnect:
                    Console.WriteLine("[MANAGER] Graceful disconnect signal received from remote.");
                    _networkService.Disconnect();
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

    public void SendSettingsToWeb()
    {
        if (_window == null) return;
        string settingsJson = GetSettingsJson();
        QueueOrSend(() =>
            _window.SendWebMessage(JsonSerializer.Serialize(new { type = "settings_data", settings = settingsJson }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
        
        // Also send current identity and discovered devices whenever settings are requested
        SendLocalIpToWeb();
        SendDiscoveredDevicesToWeb();
    }

    public void SendDiscoveredDevicesToWeb()
    {
        if (_window == null) return;
        var devices = _discoveryService.GetDiscoveredDevices();
        QueueOrSend(() =>
            _window.SendWebMessage(JsonSerializer.Serialize(new { type = "discovery_result", devices }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }

    public void SendLocalIpToWeb()
    {
        if (_window == null) return;
        string? localIp = "Unknown";
        try {
            using (var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0)) {
                socket.Connect("8.8.8.8", 65530);
                localIp = (socket.LocalEndPoint as System.Net.IPEndPoint)?.Address.ToString() ?? "Unknown";
            }
        } catch { }

        QueueOrSend(() =>
            _window!.SendWebMessage(JsonSerializer.Serialize(new { 
                type = "local_ip", 
                detail = new {
                    ip = localIp,
                    code = PairingCode
                }
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }

    private void SendUiMessage(string type, string value)
    {
        QueueOrSend(() =>
            _window!.SendWebMessage(JsonSerializer.Serialize(new { type, status = value }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }
}
