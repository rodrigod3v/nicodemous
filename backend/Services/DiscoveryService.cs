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
    private readonly List<DiscoveredDevice> _discoveredDevices = new();
    private bool _isRunning = false;
    private CancellationTokenSource? _broadcastCts;
    public string SignalingServerUrl { get; set; } = "http://localhost:5219";

    public event Action<List<DiscoveredDevice>>? OnDeviceDiscovered;

    public string PairingCode => _pairingCode;

    public DiscoveryService(string deviceName)
    {
        _deviceName = deviceName;
        _pairingCode = GeneratePairingCode();
    }

    private string GeneratePairingCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed ambiguous O, 0, I, 1
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
        Task.Run(() => RunRemoteRegistrar(_broadcastCts.Token));
        Task.Run(() => RunRemoteFetcher(_broadcastCts.Token));
    }

    public void BroadcastNow()
    {
        _broadcastCts?.Cancel();
    }

    public void TriggerRemoteFetch()
    {
        // This will cancel the Delay in RunRemoteFetcher and trigger an immediate fetch
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
        BroadcastNow(); // Refresh immediately
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
                
                // Wait 5s or until broadcast is manually triggered
                await Task.Delay(5000, token);
            }
            catch (TaskCanceledException)
            {
                // Reset CTS for next manual trigger
                _broadcastCts = new CancellationTokenSource();
                token = _broadcastCts.Token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Broadcast Error: {ex.Message}");
                await Task.Delay(1000); // Backoff
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
                            Console.WriteLine($"[DISCOVERY] Updating device info for {normalizedIp}: {existing.Code} -> {device.Code}");
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
        using var client = new HttpClient();
        while (_isRunning && !token.IsCancellationRequested)
        {
            try
            {
                string url = $"{SignalingServerUrl.TrimEnd('/')}/api/discovery/list";
                var response = await client.GetAsync(url, token);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var remoteDevices = JsonSerializer.Deserialize<List<DiscoveredDevice>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    Console.WriteLine($"[DISCOVERY] Remote Fetcher: Received {remoteDevices?.Count ?? 0} devices from {SignalingServerUrl}");
                    
                    if (remoteDevices != null)
                    {
                        bool listChanged = false;
                        lock (_discoveredDevices)
                        {
                            foreach (var device in remoteDevices)
                            {
                                // Don't add ourselves if we are in the list (though pairing code check handles this)
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
                            Console.WriteLine($"[DISCOVERY] Remote Fetcher: Discovered devices count updated to {GetDiscoveredDevices().Count}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCOVERY] Remote Fetcher Error: {ex.Message}");
            }

            await Task.Delay(10000, token); // Fetch every 10s
        }
    }

    private async Task RunRemoteRegistrar(CancellationToken token)
    {
        using var client = new HttpClient();
        while (_isRunning && !token.IsCancellationRequested)
        {
            try
            {
                var payload = new 
                { 
                    name = _deviceName, 
                    ip = GetLocalIPAddress(), 
                    code = _pairingCode 
                };
                
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                string url = $"{SignalingServerUrl.TrimEnd('/')}/api/discovery/register";
                var response = await client.PostAsync(url, content, token);
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DISCOVERY] Registered successfully as '{_deviceName}' with VM at {url}");
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[DISCOVERY] Registration failed: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCOVERY] Remote Registrar Error: {ex.Message}");
            }

            await Task.Delay(15000, token); // Register more frequently (15s) for better responsiveness
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in interfaces)
            {
                // Skip virtual, loopback and non-up interfaces
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;
                if (ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase)) continue;
                if (ni.Description.Contains("Pseudo", StringComparison.OrdinalIgnoreCase)) continue;

                var props = ni.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = addr.Address.ToString();
                        if (!string.IsNullOrEmpty(ip) && ip != "127.0.0.1")
                        {
                            return ip;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DISCOVERY] IP Detection Error: {ex.Message}");
        }

        // Final fallback
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
