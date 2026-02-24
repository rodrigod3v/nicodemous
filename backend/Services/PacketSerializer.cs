using System;
using System.Buffers.Binary;
using System.Text;

namespace Nicodemous.Backend.Services;

/// <summary>
/// Packet types used in the Nicodemous protocol.
/// Protocol framing: each packet is prefixed with a 4-byte big-endian length,
/// followed by 1-byte PacketType, followed by payload.
/// </summary>
public enum PacketType : byte
{
    MouseMove    = 0,  // Absolute move (legacy, kept for compat) — 4 bytes payload: ushort x, ushort y
    MouseClick   = 1,  // Deprecated — use MouseDown + MouseUp
    KeyPress     = 2,  // Deprecated — use KeyDown + KeyUp
    AudioFrame   = 3,
    Ping         = 4,
    Handshake    = 5,
    HandshakeAck = 6,
    MouseDown    = 7,  // 1 byte: ButtonID
    MouseUp      = 8,  // 1 byte: ButtonID
    MouseWheel   = 9,  // 4 bytes: short xDelta, short yDelta (±120 per notch)
    KeyDown      = 10, // 4 bytes: ushort keyId, ushort modifiers
    KeyUp        = 11, // 4 bytes: ushort keyId, ushort modifiers
    MouseRelMove = 12, // 4 bytes: short dx, short dy
    ClipboardPush = 13, // N bytes: UTF-8 text — sender pushes clipboard content to receiver
    ClipboardPull = 14, // 0 bytes — receiver requests sender's clipboard content
}

// Data transfer objects
public class MouseMoveData    { public ushort X { get; set; } public ushort Y { get; set; } }
public class MouseRelMoveData { public short Dx { get; set; } public short Dy { get; set; } }
public class MouseButtonData  { public byte ButtonId { get; set; } }
public class MouseWheelData   { public short XDelta { get; set; } public short YDelta { get; set; } }
public class KeyEventData     { public ushort KeyId { get; set; } public ushort Modifiers { get; set; } }
public class HandshakeData    { public string MachineName { get; set; } = ""; public string PairingCode { get; set; } = ""; }
public class AudioFrameData   { public byte[] Data { get; set; } = Array.Empty<byte>(); }
public class ClipboardData    { public string Text { get; set; } = ""; }

public static class PacketSerializer
{
    // --- Framing helpers --------------------------------------------------

    /// <summary>
    /// Wraps a raw packet (type byte + payload) in a 4-byte big-endian length prefix.
    /// This enables reliable framing over TCP.
    /// </summary>
    public static byte[] Frame(byte[] packet)
    {
        byte[] framed = new byte[4 + packet.Length];
        BinaryPrimitives.WriteInt32BigEndian(framed.AsSpan(0, 4), packet.Length);
        packet.CopyTo(framed, 4);
        return framed;
    }

    // --- Serializers ------------------------------------------------------

    public static byte[] SerializeMouseRelMove(short dx, short dy)
    {
        byte[] buf = new byte[5];
        buf[0] = (byte)PacketType.MouseRelMove;
        BinaryPrimitives.WriteInt16BigEndian(buf.AsSpan(1), dx);
        BinaryPrimitives.WriteInt16BigEndian(buf.AsSpan(3), dy);
        return Frame(buf);
    }

    public static byte[] SerializeMouseMove(ushort x, ushort y)
    {
        byte[] buf = new byte[5];
        buf[0] = (byte)PacketType.MouseMove;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(1), x);
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(3), y);
        return Frame(buf);
    }

    public static byte[] SerializeMouseDown(byte buttonId)
    {
        byte[] buf = { (byte)PacketType.MouseDown, buttonId };
        return Frame(buf);
    }

    public static byte[] SerializeMouseUp(byte buttonId)
    {
        byte[] buf = { (byte)PacketType.MouseUp, buttonId };
        return Frame(buf);
    }

    /// <summary>
    /// Mouse scroll. xDelta/yDelta: +120 = one tick forward/right, -120 = one tick back/left.
    /// </summary>
    public static byte[] SerializeMouseWheel(short xDelta, short yDelta)
    {
        byte[] buf = new byte[5];
        buf[0] = (byte)PacketType.MouseWheel;
        BinaryPrimitives.WriteInt16BigEndian(buf.AsSpan(1), xDelta);
        BinaryPrimitives.WriteInt16BigEndian(buf.AsSpan(3), yDelta);
        return Frame(buf);
    }

    public static byte[] SerializeKeyDown(ushort keyId, ushort modifiers)
    {
        byte[] buf = new byte[5];
        buf[0] = (byte)PacketType.KeyDown;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(1), keyId);
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(3), modifiers);
        return Frame(buf);
    }

    public static byte[] SerializeKeyUp(ushort keyId, ushort modifiers)
    {
        byte[] buf = new byte[5];
        buf[0] = (byte)PacketType.KeyUp;
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(1), keyId);
        BinaryPrimitives.WriteUInt16BigEndian(buf.AsSpan(3), modifiers);
        return Frame(buf);
    }

    public static byte[] SerializeHandshake(string machineName, string pairingCode)
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes(machineName);
        byte[] pinBytes = Encoding.UTF8.GetBytes(pairingCode?.PadRight(6).Substring(0, 6) ?? "000000");
        
        byte[] buf = new byte[1 + 6 + nameBytes.Length];
        buf[0] = (byte)PacketType.Handshake;
        pinBytes.CopyTo(buf, 1);
        nameBytes.CopyTo(buf, 7);
        return Frame(buf);
    }

    public static byte[] SerializeHandshakeAck()
    {
        return Frame(new byte[] { (byte)PacketType.HandshakeAck });
    }

    public static byte[] SerializePing()
    {
        return Frame(new byte[] { (byte)PacketType.Ping });
    }

    public static byte[] SerializeAudioFrame(byte[] data)
    {
        byte[] buf = new byte[1 + data.Length];
        buf[0] = (byte)PacketType.AudioFrame;
        data.CopyTo(buf, 1);
        return Frame(buf);
    }

    /// <summary>
    /// Pushes clipboard text to the other machine.
    /// Payload: UTF-8 encoded string.
    /// </summary>
    public static byte[] SerializeClipboardPush(string text)
    {
        byte[] textBytes = Encoding.UTF8.GetBytes(text ?? "");
        byte[] buf = new byte[1 + textBytes.Length];
        buf[0] = (byte)PacketType.ClipboardPush;
        textBytes.CopyTo(buf, 1);
        return Frame(buf);
    }

    /// <summary>
    /// Requests the remote machine's current clipboard content.
    /// No payload.
    /// </summary>
    public static byte[] SerializeClipboardPull()
    {
        return Frame(new byte[] { (byte)PacketType.ClipboardPull });
    }

    // --- Deserializer -----------------------------------------------------

    /// <summary>
    /// Deserializes a raw packet buffer (type byte + payload, WITHOUT the 4-byte length prefix).
    /// </summary>
    public static (PacketType type, object data) Deserialize(byte[] buffer)
    {
        if (buffer.Length == 0)
            return (PacketType.Ping, new object());

        var type = (PacketType)buffer[0];
        var payload = buffer.AsSpan(1);

        switch (type)
        {
            case PacketType.MouseRelMove:
                return (type, new MouseRelMoveData
                {
                    Dx = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(0, 2)),
                    Dy = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(2, 2)),
                });

            case PacketType.MouseMove:
                return (type, new MouseMoveData
                {
                    X = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(0, 2)),
                    Y = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(2, 2)),
                });

            case PacketType.MouseDown:
            case PacketType.MouseUp:
                return (type, new MouseButtonData { ButtonId = payload[0] });

            case PacketType.MouseWheel:
                return (type, new MouseWheelData
                {
                    XDelta = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(0, 2)),
                    YDelta = BinaryPrimitives.ReadInt16BigEndian(payload.Slice(2, 2)),
                });

            case PacketType.KeyDown:
            case PacketType.KeyUp:
                return (type, new KeyEventData
                {
                    KeyId     = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(0, 2)),
                    Modifiers = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(2, 2)),
                });

            case PacketType.Handshake:
                if (payload.Length < 6) return (type, new HandshakeData());
                return (type, new HandshakeData 
                { 
                    PairingCode = Encoding.UTF8.GetString(payload.Slice(0, 6)).Trim(),
                    MachineName = Encoding.UTF8.GetString(payload.Slice(6)) 
                });

            case PacketType.AudioFrame:
                return (type, new AudioFrameData { Data = payload.ToArray() });

            case PacketType.ClipboardPush:
                return (type, new ClipboardData { Text = Encoding.UTF8.GetString(payload) });

            case PacketType.HandshakeAck:
            case PacketType.Ping:
            case PacketType.ClipboardPull:
            default:
                return (type, new object());
        }
    }
}
