using System.Text.Json.Serialization;

namespace nicodemouse.Backend.Models;

public class UiMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class ConnectDeviceMessage : UiMessage
{
    [JsonPropertyName("ip")]
    public string? Ip { get; set; }
    
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public class ServiceToggleMessage : UiMessage
{
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;
    
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

public class UpdateSettingsMessage : UiMessage
{
    [JsonPropertyName("edge")]
    public string Edge { get; set; } = "Right";
    
    [JsonPropertyName("lockInput")]
    public bool LockInput { get; set; } = true;
    
    [JsonPropertyName("delay")]
    public int Delay { get; set; } = 150;
    
    [JsonPropertyName("cornerSize")]
    public int CornerSize { get; set; } = 50;
    
    [JsonPropertyName("sensitivity")]
    public double Sensitivity { get; set; } = 0.7;
    
    [JsonPropertyName("gestureThreshold")]
    public int GestureThreshold { get; set; } = 1000;
    
    [JsonPropertyName("pairingCode")]
    public string? PairingCode { get; set; }
    
    [JsonPropertyName("activeMonitor")]
    public string? ActiveMonitor { get; set; }
}
