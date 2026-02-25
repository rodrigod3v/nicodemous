using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace nicodemouse_server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, RegisteredDevice> Registry = new();

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisteredDevice device)
    {
        if (string.IsNullOrEmpty(device.Code) || string.IsNullOrEmpty(device.Ip))
            return BadRequest("Invalid device data.");

        device.LastSeen = DateTime.UtcNow;
        Registry[device.Code] = device;
        
        // Cleanup old entries (simple)
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        foreach (var key in Registry.Keys)
        {
            if (Registry[key].LastSeen < cutoff)
                Registry.TryRemove(key, out _);
        }

        return Ok(new { status = "registered", count = Registry.Count });
    }

    [HttpGet("resolve/{code}")]
    public IActionResult Resolve(string code)
    {
        if (Registry.TryGetValue(code, out var device))
        {
            return Ok(device);
        }
        return NotFound("Device not found or offline.");
    }

    [Authorize]
    [HttpGet("list")]
    public IActionResult List()
    {
        return Ok(Registry.Values.OrderByDescending(d => d.LastSeen));
    }
}


public class RegisteredDevice
{
    public string Name { get; set; } = "";
    public string Ip { get; set; } = "";
    public string Code { get; set; } = "";
    public DateTime LastSeen { get; set; }
}
