#if WINDOWS
using NAudio.Wave;
#endif
using Concentus;
using Concentus.Enums;
using Concentus.Structs;
using System.Net.Sockets;

namespace Nicodemous.Backend.Services;

public class AudioService
{
#if WINDOWS
    private WasapiLoopbackCapture? _capture;
#endif
    private IOpusEncoder? _encoder;
    private readonly Action<byte[]> _onAudioEncoded;
    private bool _isStreaming = false;

    public AudioService(Action<byte[]> onAudioEncoded)
    {
        _onAudioEncoded = onAudioEncoded;
        // Opus setup: 48kHz, Stereo, VoIP mode
        _encoder = OpusCodecFactory.CreateEncoder(48000, 2, OpusApplication.OPUS_APPLICATION_VOIP);
    }

    public void StartCapture()
    {
        if (_isStreaming) return;

#if WINDOWS
        _capture = new WasapiLoopbackCapture();
        _capture.DataAvailable += (s, e) =>
        {
            if (e.BytesRecorded > 0)
            {
                byte[] encoded = Encode(e.Buffer, e.BytesRecorded);
                _onAudioEncoded(encoded);
            }
        };

        _capture.StartRecording();
#else
        Console.WriteLine("Audio Capture is currently only supported on Windows.");
#endif
        _isStreaming = true;
    }

    private byte[] Encode(byte[] buffer, int length)
    {
        return buffer.Take(length).ToArray(); 
    }

    public void StopCapture()
    {
#if WINDOWS
        _capture?.StopRecording();
        _capture?.Dispose();
#endif
        _isStreaming = false;
    }
}
