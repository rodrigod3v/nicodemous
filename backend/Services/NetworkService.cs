using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nicodemous.Backend.Services;

/// <summary>
/// TCP-based network service with 4-byte big-endian length-prefixed framing.
/// 
/// Architecture:
///   - Always listens as a server on the configured port (passive side / receiver).
///   - When SetTarget is called, also opens a TCP client connection to the target (active side / controller).
///   - This means both machines run the same code; the first to call SetTarget becomes the controller.
/// </summary>
public class NetworkService : IDisposable
{
    private readonly int _port;
    private TcpListener? _listener;
    private TcpClient? _client;       // Active connection to target (controller side)
    private NetworkStream? _sendStream;

    private CancellationTokenSource _cts = new();
    private bool _disposed;

    public bool IsConnected => _client?.Connected == true && _sendStream != null;

    public event Action? OnDisconnected;

    public NetworkService(int port)
    {
        _port = port;
    }

    // -----------------------------------------------------------------------
    // Server (receive) side
    // -----------------------------------------------------------------------

    public void StartListening(Action<byte[]> onPacketReceived)
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.WriteLine($"[NETWORK] TCP listener started on port {_port}");

        Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var incoming = await _listener.AcceptTcpClientAsync(_cts.Token);
                    Console.WriteLine($"[NETWORK] Accepted connection from {incoming.Client.RemoteEndPoint}");
                    // Handle each incoming connection in its own loop
                    _ = Task.Run(() => ReceiveLoop(incoming, onPacketReceived, _cts.Token));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        Console.WriteLine($"[NETWORK] Accept error: {ex.Message}");
                }
            }
        }, _cts.Token);
    }

    private static async Task ReceiveLoop(TcpClient tcp, Action<byte[]> onPacket, CancellationToken ct)
    {
        using var stream = tcp.GetStream();
        byte[] lenBuf = new byte[4];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Read 4-byte length prefix
                await ReadExact(stream, lenBuf, 4, ct);
                int len = BinaryPrimitives.ReadInt32BigEndian(lenBuf);

                if (len <= 0 || len > 4 * 1024 * 1024)
                {
                    Console.WriteLine($"[NETWORK] Invalid packet length {len}, dropping connection.");
                    break;
                }

                byte[] payload = new byte[len];
                await ReadExact(stream, payload, len, ct);

                // Log non-mouse-move packets
                if (payload.Length > 0 && payload[0] != 0 && payload[0] != 12)
                    Console.WriteLine($"[NETWORK] Packet type={payload[0]}, len={len}");

                onPacket(payload);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"[NETWORK] Receive loop ended: {ex.Message}");
        }
    }

    private static async Task ReadExact(NetworkStream stream, byte[] buf, int count, CancellationToken ct)
    {
        int read = 0;
        while (read < count)
        {
            int n = await stream.ReadAsync(buf.AsMemory(read, count - read), ct);
            if (n == 0) throw new EndOfStreamException("Connection closed by remote.");
            read += n;
        }
    }

    // -----------------------------------------------------------------------
    // Client (send) side
    // -----------------------------------------------------------------------

    public void SetTarget(string ipAddress, int port)
    {
        // Disconnect any previous connection
        DisconnectClient();

        if (!IPAddress.TryParse(ipAddress, out var addr))
        {
            Console.WriteLine($"[NETWORK] Invalid target IP: {ipAddress}");
            return;
        }

        Task.Run(async () =>
        {
            const int maxRetries = 5;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var tcp = new TcpClient();
                    tcp.NoDelay = true; // Disable Nagle for low-latency input events
                    await tcp.ConnectAsync(addr, port);
                    _client = tcp;
                    _sendStream = tcp.GetStream();
                    Console.WriteLine($"[NETWORK] Connected to {ipAddress}:{port}");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NETWORK] Connect attempt {i + 1} failed: {ex.Message}. Retrying in 1s...");
                    await Task.Delay(1000);
                }
            }
            Console.WriteLine($"[NETWORK] Could not connect to {ipAddress}:{port} after {maxRetries} attempts.");
            OnDisconnected?.Invoke();
        });
    }

    public void Send(byte[] framedPacket)
    {
        var stream = _sendStream;
        if (stream == null) return;
        try
        {
            // Write is not thread-safe; lock micro-scope around the write
            lock (stream)
            {
                stream.Write(framedPacket, 0, framedPacket.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NETWORK] Send error: {ex.Message}. Disconnecting.");
            DisconnectClient();
            OnDisconnected?.Invoke();
        }
    }

    private void DisconnectClient()
    {
        try { _sendStream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
        _sendStream = null;
        _client = null;
    }

    public void Stop()
    {
        _cts.Cancel();
        try { _listener?.Stop(); } catch { }
        DisconnectClient();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts.Dispose();
    }
}
