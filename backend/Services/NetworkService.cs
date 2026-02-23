using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class NetworkService
{
    private readonly UdpClient _udpClient;
    private readonly int _port;
    private IPEndPoint? _targetEndPoint;
    private bool _isRunning;

    public bool HasTarget => _targetEndPoint != null;

    public NetworkService(int port)
    {
        _port = port;
        _udpClient = new UdpClient(_port);
        
        // On Windows, ignore "Connection Reset" error from ICMP Port Unreachable
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            _udpClient.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { 0 }, null);
        }
    }

    public void SetTarget(string ipAddress, int port)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _targetEndPoint = null;
            return;
        }

        if (IPAddress.TryParse(ipAddress, out var parsedAddr))
        {
            _targetEndPoint = new IPEndPoint(parsedAddr, port);
        }
        else
        {
            Console.WriteLine($"[NETWORK] Attempted to set invalid target IP: {ipAddress}");
            _targetEndPoint = null;
        }
    }

    public void StartListening(Action<byte[]> onDataReceived)
    {
        _isRunning = true;
        Task.Run(async () =>
        {
            Console.WriteLine($"[NETWORK] Listener started on port {_port}");
            while (_isRunning)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    if (result.Buffer.Length > 0)
                    {
                        // Trace log for specific packet types to verify flow
                        byte type = result.Buffer[0];
                        if (type != 0) // Don't spam mouse moves, but log Handshake/Clicks
                        {
                            Console.WriteLine($"[NETWORK] Packet received from {result.RemoteEndPoint}: Type {type}, Size {result.Buffer.Length}");
                        }
                        onDataReceived(result.Buffer);
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning) Console.WriteLine($"[NETWORK] UDP Receive Error: {ex.Message}");
                }
            }
        });
    }

    public void Send(byte[] data)
    {
        if (_targetEndPoint == null) return;
        _udpClient.Send(data, data.Length, _targetEndPoint);
    }

    public void Stop()
    {
        _isRunning = false;
        _udpClient.Close();
    }
}
