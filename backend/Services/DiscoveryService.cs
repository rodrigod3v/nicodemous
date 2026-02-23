using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Nicodemous.Backend.Services;

public class DiscoveryService
{
    private const int DiscoveryPort = 8889;
    private const string MulticastGroup = "239.0.0.1";
    private readonly string _deviceName;
    private readonly string _pairingCode;
    private readonly List<DiscoveredDevice> _discoveredDevices = new();
    private bool _isRunning = false;
    private CancellationTokenSource? _broadcastCts;

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
    }

    public void BroadcastNow()
    {
        _broadcastCts?.Cancel();
    }

    public void Stop()
    {
        _isRunning = false;
        _broadcastCts?.Cancel();
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
                        var existing = _discoveredDevices.FirstOrDefault(d => d.Code == device.Code);
                        if (existing == null) 
                        {
                            _discoveredDevices.Add(device);
                            listChanged = true;
                        }
                        else if (existing.Ip != device.Ip)
                        {
                            existing.Ip = device.Ip;
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

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        return host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "127.0.0.1";
    }
}

public class DiscoveredDevice
{
    public string Name { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Code { get; set; } = "";
}
