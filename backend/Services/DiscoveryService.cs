using Zeroconf;
using System.Net;
using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class DiscoveryService
{
    private const string ServiceType = "_nicodemous._tcp.local.";
    private readonly string _deviceName;
    private readonly int _port;

    public DiscoveryService(string deviceName, int port)
    {
        _deviceName = deviceName;
        _port = port;
    }

    public async Task Advertise()
    {
        // Note: Zeroconf library usually handles browsing. 
        // For advertising in .NET, it's often easiest to use a simple MDNS implementation
        // or just announce via UDP Multicast if Zeroconf doesn't provide a direct "Announce" API.
        // For now, we simulate the logic as we will refine it with a specific mDNS advertiser package if needed.
        Console.WriteLine($"Advertising Nicodemous service: {_deviceName} at port {_port}");
        await Task.CompletedTask;
    }

    public async Task<List<DiscoveredDevice>> Browse()
    {
        IReadOnlyList<IZeroconfHost> results = await ZeroconfResolver.ResolveAsync(ServiceType);
        return results.Select(r => new DiscoveredDevice
        {
            Name = r.DisplayName,
            IPAddress = r.IPAddress,
            Id = r.Id
        }).ToList();
    }
}

public class DiscoveredDevice
{
    public string Name { get; set; } = "";
    public string IPAddress { get; set; } = "";
    public string Id { get; set; } = "";
}
