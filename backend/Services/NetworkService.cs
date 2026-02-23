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

    public NetworkService(int port)
    {
        _port = port;
        _udpClient = new UdpClient(_port);
    }

    public void SetTarget(string ipAddress, int port)
    {
        _targetEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
    }

    public void StartListening(Action<string> onMessageReceived)
    {
        _isRunning = true;
        Task.Run(async () =>
        {
            while (_isRunning)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    string message = Encoding.UTF8.GetString(result.Buffer);
                    onMessageReceived(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP Receive Error: {ex.Message}");
                }
            }
        });
    }

    public void Send(string message)
    {
        if (_targetEndPoint == null) return;

        byte[] data = Encoding.UTF8.GetBytes(message);
        _udpClient.Send(data, data.Length, _targetEndPoint);
    }

    public void Stop()
    {
        _isRunning = false;
        _udpClient.Close();
    }
}
