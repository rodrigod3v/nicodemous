using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace nicodemouse.Backend.Services;

public class DiscoveryService
{
    private const int DiscoveryPort = 8889;
    private const string MulticastGroup = "239.0.0.1";
    private readonly string _deviceName;
    private string _pairingCode;
    private string? _signalingServerUrl;
    private readonly List<DiscoveredDevice> _discoveredDevices = new();
    private bool _isRunning = false;
    private CancellationTokenSource? _broadcastCts;
    private readonly HttpClient _httpClient = new();

    public event Action<List<DiscoveredDevice>>? OnDeviceDiscovered;

    public string PairingCode => _pairingCode;
    public string SignalingServerUrl { get; set; } = "http://144.22.254.132:8080";

    public DiscoveryService(string deviceName)
    {
        _deviceName = deviceName;
        _pairingCode = GeneratePairingCode();
    }

    private string GeneratePairingCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; 
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _broadcastCts = new CancellationTokenSource();

        Task.Run(() => RunBroadcaster(_broadcastCts.Token));
        Task.Run(RunListener);
        
        // Use the setting URL
        _signalingServerUrl = SignalingServerUrl;

        if (!string.IsNullOrEmpty(_signalingServerUrl))
        {
            Task.Run(() => RunRemoteRegistrar(_broadcastCts.Token));
            Task.Run(() => RunRemoteFetcher(_broadcastCts.Token));
        }
    }

    public void SetSignalingServerUrl(string url)
    {
        SignalingServerUrl = url;
        _signalingServerUrl = url;
        // Optionally trigger immediate fetch/register here if needed
    }

    public void BroadcastNow()
    {
        _broadcastCts?.Cancel();
    }

    public void TriggerRemoteFetch()
    {
        _broadcastCts?.Cancel(); 
    }

    public void Stop()
    {
        _isRunning = false;
        _broadcastCts?.Cancel();
    }

    public void UpdatePairingCode(string newCode)
    {
        _pairingCode = newCode;
        BroadcastNow();
    }

    private async Task RunBroadcaster(CancellationToken token)
    {
        var endPoint = new IPEndPoint(IPAddress.Parse(MulticastGroup), DiscoveryPort);
        
        while (_isRunning)
        {
            try 
            {
                using var client = new UdpClient();
                var packet = JsonSerializer.Serialize(new { 
                    name = _deviceName, 
                    code = _pairingCode, 
                    ip = GetLocalIPAddress() 
                });
                byte[] data = Encoding.UTF8.GetBytes(packet);

                await client.SendAsync(data, data.Length, endPoint);
                await Task.Delay(5000, token);
            }
            catch (TaskCanceledException)
            {
                _broadcastCts = new CancellationTokenSource();
                token = _broadcastCts.Token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Broadcast Error: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    private async Task RunListener()
    {
        using var client = new UdpClient();
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
        client.JoinMulticastGroup(IPAddress.Parse(MulticastGroup));

        while (_isRunning)
        {
            try
            {
                var result = await client.ReceiveAsync();
                var json = Encoding.UTF8.GetString(result.Buffer);
                var device = JsonSerializer.Deserialize<DiscoveredDevice>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (device != null && device.Code != _pairingCode)
                {
                    bool listChanged = false;
                    lock (_discoveredDevices)
                    {
                        string normalizedIp = device.Ip.Trim();
                        var existing = _discoveredDevices.FirstOrDefault(d => d.Ip.Trim().Equals(normalizedIp, StringComparison.OrdinalIgnoreCase));
                        
                        if (existing == null) 
                        {
                            Console.WriteLine($"[DISCOVERY] New device found: {device.Name} at {normalizedIp}");
                            _discoveredDevices.Add(device);
                            listChanged = true;
                        }
                        else if (!existing.Code.Equals(device.Code, StringComparison.OrdinalIgnoreCase) || 
                                 !existing.Name.Equals(device.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            existing.Code = device.Code;
                            existing.Name = device.Name;
                            listChanged = true;
                        }
                    }

                    if (listChanged)
                    {
                        OnDeviceDiscovered?.Invoke(GetDiscoveredDevices());
                    }
                }
            }
            catch { /* Ignore listener errors */ }
        }
    }

    public async Task<string?> ResolveCodeAsync(string code)
    {
        var localIp = GetIpByCode(code);
        if (localIp != null) return localIp;

        if (string.IsNullOrEmpty(_signalingServerUrl)) return null;

        try
        {
            var response = await _httpClient.GetAsync($"{_signalingServerUrl.TrimEnd('/')}/api/discovery/resolve/{code}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var device = JsonSerializer.Deserialize<DiscoveredDevice>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return device?.Ip;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DISCOVERY] Cloud resolution error: {ex.Message}");
        }

        return null;
    }

    public string? GetIpByCode(string code)
    {
        lock (_discoveredDevices)
        {
            return _discoveredDevices.FirstOrDefault(d => d.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Ip;
        }
    }

    public List<DiscoveredDevice> GetDiscoveredDevices()
    {
        lock (_discoveredDevices)
        {
            return _discoveredDevices.ToList();
        }
    }

    public void ClearDiscoveredDevices()
    {
        lock (_discoveredDevices)
        {
            _discoveredDevices.Clear();
        }
    }

    private async Task RunRemoteFetcher(CancellationToken token)
    {
        while (_isRunning && !token.IsCancellationRequested)
        {
            try
            {
                if (string.IsNullOrEmpty(_signalingServerUrl)) {
                    await Task.Delay(5000, token);
                    continue;
                }

                string url = $"{_signalingServerUrl.TrimEnd('/')}/api/discovery/list";
                var response = await _httpClient.GetAsync(url, token);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var remoteDevices = JsonSerializer.Deserialize<List<DiscoveredDevice>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (remoteDevices != null)
                    {
                        bool listChanged = false;
                        lock (_discoveredDevices)
                        {
                            foreach (var device in remoteDevices)
                            {
                                if (device.Code == _pairingCode) continue;

                                string normalizedIp = device.Ip.Trim();
                                var existing = _discoveredDevices.FirstOrDefault(d => d.Ip.Trim().Equals(normalizedIp, StringComparison.OrdinalIgnoreCase));
                                
                                if (existing == null)
                                {
                                    _discoveredDevices.Add(device);
                                    listChanged = true;
                                }
                                else if (!existing.Code.Equals(device.Code, StringComparison.OrdinalIgnoreCase))
                                {
                                    existing.Code = device.Code;
                                    existing.Name = device.Name;
                                    listChanged = true;
                                }
                            }
                        }

                        if (listChanged)
                        {
                            OnDeviceDiscovered?.Invoke(GetDiscoveredDevices());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCOVERY] Remote Fetcher Error: {ex.Message}");
            }

            await Task.Delay(10000, token);
        }
    }

    private async Task RunRemoteRegistrar(CancellationToken token)
    {
        while (_isRunning && !token.IsCancellationRequested)
        {
            try
            {
                if (string.IsNullOrEmpty(_signalingServerUrl)) {
                    await Task.Delay(5000, token);
                    continue;
                }

                var payload = new 
                { 
                    name = _deviceName, 
                    ip = GetLocalIPAddress(), 
                    code = _pairingCode 
                };
                
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                string url = $"{_signalingServerUrl.TrimEnd('/')}/api/discovery/register";
                var response = await _httpClient.PostAsync(url, content, token);
                
                if (response.IsSuccessStatusCode)
                {
                    // Silent success for registrar to avoid log spam
                }
                else
                {
                    Console.WriteLine($"[DISCOVERY] Registration failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCOVERY] Remote Registrar Error: {ex.Message}");
            }

            await Task.Delay(15000, token);
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
                if (ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase)) continue;
                
                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = addr.Address.ToString();
                        if (!string.IsNullOrEmpty(ip) && ip != "127.0.0.1") return ip;
                    }
                }
            }
        }
        catch { }

        try 
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }
}

public class DiscoveredDevice
{
    public string Name { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Code { get; set; } = "";
}
