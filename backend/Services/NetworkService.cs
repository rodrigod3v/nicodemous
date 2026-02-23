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
            while (_isRunning)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    onDataReceived(result.Buffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP Receive Error: {ex.Message}");
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
