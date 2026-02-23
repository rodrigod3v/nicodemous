# if WINDOWS
using NAudio.Wave;
# endif
using Concentus;
using Concentus.Enums;
using System.Text;

namespace Nicodemous.Backend.Services;

public class AudioReceiveService
{
# if WINDOWS
    private readonly WaveOutEvent _waveOut;
    private readonly BufferedWaveProvider _waveProvider;
# endif
    private readonly IOpusDecoder _decoder;
    private bool _isPlaying = false;

    public AudioReceiveService()
    {
# if WINDOWS
        // Setup playback: 48kHz, 16-bit, Stereo
        var waveFormat = new WaveFormat(48000, 16, 2);
        _waveProvider = new BufferedWaveProvider(waveFormat)
        {
            DiscardOnBufferOverflow = true,
            BufferDuration = TimeSpan.FromMilliseconds(500)
        };

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_waveProvider);
# endif

        // Opus Decoder setup
        _decoder = OpusCodecFactory.CreateDecoder(48000, 2);
    }

    public void Start()
    {
        if (_isPlaying) return;
# if WINDOWS
        _waveOut.Play();
# endif
        _isPlaying = true;
    }

    public void Stop()
    {
# if WINDOWS
        _waveOut.Stop();
        _waveProvider.ClearBuffer();
# endif
        _isPlaying = false;
    }

    public void ProcessFrame(byte[] encodedData)
    {
        if (!_isPlaying) return;

        try
        {
# if WINDOWS
            _waveProvider.AddSamples(encodedData, 0, encodedData.Length);
# else
            // Playback on Mac would require a different library like PortAudio or Soundio
# endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio Playback Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
# if WINDOWS
        _waveOut.Dispose();
# endif
    }
}
