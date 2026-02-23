using NAudio.Wave;
using Concentus.Enums;
using Concentus.Structs;
using System.Net.Sockets;

namespace Nicodemous.Backend.Services;

public class AudioService
{
    private WasapiLoopbackCapture? _capture;
    private OpusEncoder? _encoder;
    private readonly Action<byte[]> _onAudioEncoded;
    private bool _isStreaming = false;

    public AudioService(Action<byte[]> onAudioEncoded)
    {
        _onAudioEncoded = onAudioEncoded;
        // Opus setup: 48kHz, Stereo, VoIP mode
        _encoder = new OpusEncoder(48000, 2, OpusApplication.OPUS_APPLICATION_VOIP);
    }

    public void StartCapture()
    {
        if (_isStreaming) return;

        _capture = new WasapiLoopbackCapture();
        _capture.DataAvailable += (s, e) =>
        {
            if (e.BytesRecorded > 0)
            {
                // In a real app, we convert to PCM float/short, encode with Opus and send
                // This is a simplified version for the MVP walkthrough
                byte[] encoded = Encode(e.Buffer, e.BytesRecorded);
                _onAudioEncoded(encoded);
            }
        };

        _capture.StartRecording();
        _isStreaming = true;
    }

    private byte[] Encode(byte[] buffer, int length)
    {
        // Placeholder for Opus encoding logic
        // In full implementation, we'd use Concentus to compress the frame
        return buffer.Take(length).ToArray(); 
    }

    public void StopCapture()
    {
        _capture?.StopRecording();
        _capture?.Dispose();
        _isStreaming = false;
    }
}
