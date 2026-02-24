using System;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
    private Stream? _sendStream;      // SslStream or NetworkStream
    private Action<byte[]>? _onPacketReceived;
    private static readonly X509Certificate2 _serverCert = GenerateSelfSignedCertificate();

    private CancellationTokenSource _cts = new();
    private bool _disposed;

    public bool IsConnected => _client?.Connected == true && _sendStream != null;

    public event Action<bool>? OnConnected; // true if incoming (controlled), false if outgoing (controller)
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
        _onPacketReceived = onPacketReceived; // Store for client-side use
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
                    incoming.NoDelay = true;
                    Console.WriteLine($"[NETWORK] Accepted connection from {incoming.Client.RemoteEndPoint}");

                    // Save the stream so Send() works bidirectionally from the listener side too.
                    // Replaces any stale previous connection.
                    Disconnect();
                    _client = incoming;
                    var netStream = incoming.GetStream();
                    
                    // Upgrade to SSL
                    var sslStream = new SslStream(netStream, false);
                    await sslStream.AuthenticateAsServerAsync(_serverCert, false, false);
                    
                    _sendStream = sslStream;
                    Console.WriteLine($"[NETWORK] TLS Handshake complete (Incoming).");
                    
                    Console.WriteLine($"[NETWORK] Triggering OnConnected(isIncoming: true).");
                    OnConnected?.Invoke(true);

                    // Handle receive in its own task
                    _ = Task.Run(() => ReceiveLoop(incoming, sslStream, onPacketReceived, _cts.Token));
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

    private static async Task ReceiveLoop(TcpClient tcp, Stream stream, Action<byte[]> onPacket, CancellationToken ct)
    {
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
                    Console.WriteLine($"[NETWORK] Packet received: type={payload[0]}, len={len}");

                onPacket(payload);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"[NETWORK] Receive loop ended: {ex.Message}");
        }
    }

    private static async Task ReadExact(Stream stream, byte[] buf, int count, CancellationToken ct)
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
                    var netStream = tcp.GetStream();

                    // Upgrade to SSL
                    var sslStream = new SslStream(netStream, false, (s, c, ch, e) => true); // Trust peer self-signed
                    await sslStream.AuthenticateAsClientAsync(ipAddress);
                    
                    _sendStream = sslStream;
                    Console.WriteLine($"[NETWORK] TLS Handshake complete (Outgoing). Connected to {ipAddress}:{port}");
                    
                    // Start receiving from the controller side too!
                    if (_onPacketReceived != null)
                        _ = Task.Run(() => ReceiveLoop(tcp, sslStream, _onPacketReceived, _cts.Token));
                    
                    OnConnected?.Invoke(false);
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
            Disconnect();
            OnDisconnected?.Invoke();
        }
    }

    public void Disconnect()
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
        Disconnect();
    }

    private static X509Certificate2 GenerateSelfSignedCertificate()
    {
        try
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("cn=Nicodemous", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var cert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));
            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NETWORK] Cert generation failed: {ex.Message}");
            return null!;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts.Dispose();
    }
}
