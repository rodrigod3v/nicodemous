using System;
using System.Text;

namespace Nicodemous.Backend.Services;

public enum PacketType : byte
{
    MouseMove = 0,
    MouseClick = 1,
    KeyPress = 2,
    AudioFrame = 3,
    Ping = 4,
    Handshake = 5,
    HandshakeAck = 6,
    MouseDown = 7,
    MouseUp = 8,
    MouseWheel = 9
}

public class MouseMoveData { public ushort X { get; set; } public ushort Y { get; set; } }
public class MouseClickData { public string Button { get; set; } = "Left"; }
public class KeyPressData { public string Key { get; set; } = ""; }
public class HandshakeData { public string MachineName { get; set; } = ""; }
public class MouseWheelData { public short Delta { get; set; } }

public static class PacketSerializer
{
    public static byte[] SerializeMouseMove(ushort x, ushort y)
    {
        byte[] buffer = new byte[5];
        buffer[0] = (byte)PacketType.MouseMove;
        BitConverter.TryWriteBytes(buffer.AsSpan(1), x);
        BitConverter.TryWriteBytes(buffer.AsSpan(3), y);
        return buffer;
    }

    public static byte[] SerializeMouseClick(string button)
    {
        byte[] buttonBytes = Encoding.UTF8.GetBytes(button);
        byte[] buffer = new byte[1 + buttonBytes.Length];
        buffer[0] = (byte)PacketType.MouseClick;
        Buffer.BlockCopy(buttonBytes, 0, buffer, 1, buttonBytes.Length);
        return buffer;
    }

    public static byte[] SerializeMouseDown(string button)
    {
        byte[] buttonBytes = Encoding.UTF8.GetBytes(button);
        byte[] buffer = new byte[1 + buttonBytes.Length];
        buffer[0] = (byte)PacketType.MouseDown;
        Buffer.BlockCopy(buttonBytes, 0, buffer, 1, buttonBytes.Length);
        return buffer;
    }

    public static byte[] SerializeMouseUp(string button)
    {
        byte[] buttonBytes = Encoding.UTF8.GetBytes(button);
        byte[] buffer = new byte[1 + buttonBytes.Length];
        buffer[0] = (byte)PacketType.MouseUp;
        Buffer.BlockCopy(buttonBytes, 0, buffer, 1, buttonBytes.Length);
        return buffer;
    }

    public static byte[] SerializeMouseWheel(short delta)
    {
        byte[] buffer = new byte[3];
        buffer[0] = (byte)PacketType.MouseWheel;
        BitConverter.TryWriteBytes(buffer.AsSpan(1), delta);
        return buffer;
    }

    public static byte[] SerializeKeyPress(string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] buffer = new byte[1 + keyBytes.Length];
        buffer[0] = (byte)PacketType.KeyPress;
        Buffer.BlockCopy(keyBytes, 0, buffer, 1, keyBytes.Length);
        return buffer;
    }

    public static byte[] SerializeAudioFrame(byte[] data)
    {
        byte[] buffer = new byte[1 + data.Length];
        buffer[0] = (byte)PacketType.AudioFrame;
        Buffer.BlockCopy(data, 0, buffer, 1, data.Length);
        return buffer;
    }

    public static byte[] SerializeHandshake(string machineName)
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes(machineName);
        byte[] buffer = new byte[1 + nameBytes.Length];
        buffer[0] = (byte)PacketType.Handshake;
        Buffer.BlockCopy(nameBytes, 0, buffer, 1, nameBytes.Length);
        return buffer;
    }

    public static (PacketType type, object data) Deserialize(byte[] buffer)
    {
        PacketType type = (PacketType)buffer[0];
        Span<byte> payload = buffer.AsSpan(1);

        switch (type)
        {
            case PacketType.MouseMove:
                ushort x = BitConverter.ToUInt16(payload.Slice(0, 2));
                ushort y = BitConverter.ToUInt16(payload.Slice(2, 2));
                return (type, new MouseMoveData { X = x, Y = y });
            
            case PacketType.MouseClick:
                return (type, new MouseClickData { Button = Encoding.UTF8.GetString(payload) });

            case PacketType.KeyPress:
                return (type, new KeyPressData { Key = Encoding.UTF8.GetString(payload) });

            case PacketType.AudioFrame:
                return (type, payload.ToArray());

            case PacketType.Handshake:
                return (type, new HandshakeData { MachineName = Encoding.UTF8.GetString(payload) });

            case PacketType.MouseDown:
            case PacketType.MouseUp:
                return (type, new MouseClickData { Button = Encoding.UTF8.GetString(payload) });

            case PacketType.MouseWheel:
                return (type, new MouseWheelData { Delta = BitConverter.ToInt16(payload) });

            default:
                return (type, new { });
        }
    }
}
