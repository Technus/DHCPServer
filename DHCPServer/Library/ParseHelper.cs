using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace GitHub.JPMikkers.DHCP;

public static class ParseHelper
{
    public static IPAddress ReadIPAddress(ReadOnlySpan<byte> s)
    {
        return new IPAddress(s);
    }

    public static IPAddress ReadIPAddress(Stream s)
    {
        var bytes = new byte[4];
        if(s.Read(bytes, 0, bytes.Length) is not 4)
            throw new InvalidOperationException("Stream did not contain 4 bytes");
        return new IPAddress(bytes);
    }

    public static void WriteIPAddress(Stream s, IPAddress v)
    {
        var bytes = v.GetAddressBytes();
        s.Write(bytes, 0, bytes.Length);
    }

    public static byte ReadUInt8(Stream s)
    {
        using var br = new BinaryReader(s);
        return br.ReadByte();
    }

    public static void WriteUInt8(Stream s, byte v)
    {
        using var bw = new BinaryWriter(s);
        bw.Write(v);
    }

    public static ushort ReadUInt16(ReadOnlySpan<byte> s)
    {
        return (ushort)IPAddress.NetworkToHostOrder(MemoryMarshal.Read<ushort>(s));
    }

    public static ushort ReadUInt16(Stream s)
    {
        using var br = new BinaryReader(s);
        return (ushort)IPAddress.NetworkToHostOrder((short)br.ReadUInt16());
    }

    public static void WriteUInt16(Stream s, ushort v)
    {
        using var bw = new BinaryWriter(s);
        bw.Write((ushort)IPAddress.HostToNetworkOrder((short)v));
    }

    public static uint ReadUInt32(ReadOnlySpan<byte> s)
    {
        return (uint)IPAddress.NetworkToHostOrder(MemoryMarshal.Read<uint>(s));
    }

    public static uint ReadUInt32(Stream s)
    {
        using var br = new BinaryReader(s);
        return (uint)IPAddress.NetworkToHostOrder((int)br.ReadUInt32());
    }

    public static void WriteUInt32(Stream s, uint v)
    {
        using var bw = new BinaryWriter(s);
        bw.Write((uint)IPAddress.HostToNetworkOrder((int)v));
    }

    public static string ReadZString(ReadOnlySpan<byte> s)
    {
        var nul = s.IndexOf((byte)0);
        if(nul is 0)
            return string.Empty;
        return Encoding.ASCII.GetString(nul is -1 ? s : s[..(nul-1)]);
    }

    public static string ReadZString(Stream s)
    {
        var sb = new StringBuilder();
        int c = s.ReadByte();
        while(c > 0)
        {
            sb.Append((char)c);
            c = s.ReadByte();
        }
        return sb.ToString();
    }

    public static void WriteZString(Stream s, string msg)
    {
        using var tw = new StreamWriter(s, Encoding.ASCII);
        tw.Write(msg);
        tw.Flush();
        s.WriteByte(0);
    }

    public static void WriteZString(Stream s, string msg, int length)
    {
        if(msg.Length >= length)
        {
            msg = msg.Substring(0, length - 1);
        }

        using var tw = new StreamWriter(s, Encoding.ASCII);
        tw.Write(msg);
        tw.Flush();

        // write terminating and padding zero's
        for(int t = msg.Length; t < length; t++)
        {
            s.WriteByte(0);
        }
    }

    public static string ReadString(Stream s, int maxLength)
    {
        var sb = new StringBuilder();
        int c = s.ReadByte();
        while(c > 0 && sb.Length < maxLength)
        {
            sb.Append((char)c);
            c = s.ReadByte();
        }
        return sb.ToString();
    }

    public static string ReadString(Stream s)
    {
        return ReadString(s, 16 * 1024);
    }

    public static void WriteString(Stream s, string msg, bool zeroTerminated = false)
    {
        using var tw = new StreamWriter(s, Encoding.ASCII);
        tw.Write(msg);
        tw.Flush();
        if(zeroTerminated) 
            s.WriteByte(0);
    }
}
