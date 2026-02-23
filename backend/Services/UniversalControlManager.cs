using System.Text.Json;

namespace Nicodemous.Backend.Services;

public class UniversalControlManager
{
    private readonly InputService _inputService;
    private readonly NetworkService _networkService;
    private readonly InjectionService _injectionService;
    
    private bool _isRemoteControlActive = false;
    private int _screenWidth = 1920; // Placeholder, should be detected

    public UniversalControlManager()
    {
        _injectionService = new InjectionService();
        _networkService = new NetworkService(8888);
        _inputService = new InputService(HandleLocalInput);

        _networkService.StartListening(HandleRemoteInput);
    }

    public void Start()
    {
        _inputService.Start();
    }

    public void Stop()
    {
        _inputService.Stop();
        _networkService.Stop();
    }

    public void ConnectTo(string ip)
    {
        _networkService.SetTarget(ip, 8888);
    }

    private void HandleLocalInput(string json)
    {
        var doc = JsonDocument.Parse(json);
        string? type = doc.RootElement.GetProperty("type").GetString();

        if (type == "mouse_move")
        {
            short x = doc.RootElement.GetProperty("x").GetInt16();
            
            // Basic edge detection (Right edge)
            if (!_isRemoteControlActive && x >= _screenWidth - 5)
            {
                _isRemoteControlActive = true;
                Console.WriteLine("Switched to Remote Control");
            }
            else if (_isRemoteControlActive && x < 5)
            {
                _isRemoteControlActive = false;
                Console.WriteLine("Switched to Local Control");
            }
        }

        if (_isRemoteControlActive)
        {
            _networkService.Send(json);
        }
    }

    private void HandleRemoteInput(string json)
    {
        // If we are the remote, inject what we receive
        _injectionService.Inject(json);
    }
}
