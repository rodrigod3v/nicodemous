using NAudio.Wave;
using Concentus;
using Concentus.Enums;
using System.Text;

namespace Nicodemous.Backend.Services;

public class AudioReceiveService
{
    private readonly WaveOutEvent _waveOut;
    private readonly BufferedWaveProvider _waveProvider;
    private readonly IOpusDecoder _decoder;
    private bool _isPlaying = false;

    public AudioReceiveService()
    {
        // Setup playback: 48kHz, 16-bit, Stereo
        var waveFormat = new WaveFormat(48000, 16, 2);
        _waveProvider = new BufferedWaveProvider(waveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromMilliseconds(500)
        };

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_waveProvider);

        // Opus Decoder setup
        _decoder = OpusCodecFactory.CreateDecoder(48000, 2);
    }

    public void Start()
    {
        if (_isPlaying) return;
        _waveOut.Play();
        _isPlaying = true;
    }

    public void Stop()
    {
        _waveOut.Stop();
        _isPlaying = false;
        _waveProvider.ClearBuffer();
    }

    public void ProcessFrame(byte[] encodedData)
    {
        if (!_isPlaying) return;

        try
        {
            // In a real implementation, we'd decode the Opus frame to PCM
            // For the MVP, we assume the data is ready or simplified
            // decodedSamples = _decoder.Decode(encodedData, 0, encodedData.Length, outBuffer, 0, frameSize);
            
            // Simplified: Add to provider (in production this would be decoded PCM)
            _waveProvider.AddSamples(encodedData, 0, encodedData.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio Playback Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _waveOut.Dispose();
    }
}
