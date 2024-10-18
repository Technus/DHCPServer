using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace DHCP.Server.Library;

public static class Utils
{
    public static string GetSettingsPath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var old = Path.Combine(root, "JPMikkers");
        if(Directory.Exists(old))
            return Path.Combine(old, "DHCP Server");
        return Path.Combine(root, "DHCsharP Server");
    }

    public static TimeSpan InfiniteTimeSpan { get; } = TimeSpan.FromSeconds(uint.MaxValue);

    public static bool IsInfiniteTimeSpan(this TimeSpan timeSpan) =>
        timeSpan < TimeSpan.FromSeconds(0) || timeSpan >= InfiniteTimeSpan;

    public static TimeSpan SanitizeTimeSpan(this TimeSpan timeSpan) =>
        timeSpan.IsInfiniteTimeSpan() ?
            InfiniteTimeSpan :
            timeSpan;

    public static bool ByteArraysAreEqual(byte[] array1, byte[] array2) =>
        array1.Length == array2.Length &&
        array1.AsSpan().SequenceEqual(array2.AsSpan());

    public static string BytesToHexString(byte[] data, string separator) =>
        string.IsNullOrEmpty(separator) ?
            Convert.ToHexString(data) :
            string.Join(separator, Convert.ToHexString(data).Chunk(2).Select(x => new string(x)));

    public static byte[] HexStringToBytes(string data)
    {
        // strip separator chars, anything not 0-9/A-F
        var sb = data.Where(char.IsAsciiHexDigit).ToArray().AsSpan();
        // remove spurious trailing nibble before converting
        return Convert.FromHexString(sb[..(sb.Length & ~1)]);
    }

    public static IPAddress GetSubnetMask(this IPAddress address)
    {
        foreach(NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            foreach(UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
            {
                if(unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork &&
                    address.Equals(unicastIPAddressInformation.Address))
                {
                    // the following mask can be null.. return 255.255.255.0 in that case
                    return unicastIPAddressInformation.IPv4Mask ?? new IPAddress(new byte[] { 255, 255, 255, 0 });
                }
            }
        }
        throw new ArgumentException($"Can't find subnetmask for IP address '{address}'");
    }

    public static IPAddress UInt32ToIPAddress(this uint address) => new([
            (byte)(address>>24 & 0xFF),
            (byte)(address>>16 & 0xFF),
            (byte)(address>> 8 & 0xFF),
            (byte)(address>> 0 & 0xFF),
        ]);

    public static uint IPAddressToUInt32(this IPAddress address) =>
            (uint)address.GetAddressBytes()[0] << 24 |
            (uint)address.GetAddressBytes()[1] << 16 |
            (uint)address.GetAddressBytes()[2] << 8 |
            (uint)address.GetAddressBytes()[3] << 0;

    public static string PrefixLines(string src, string prefix)
    {
        var sb = new StringBuilder();
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms);
        sw.Write(src);
        sw.Flush();
        ms.Position = 0;
        using var sr = new StreamReader(ms);
        string? line;
        while((line = sr.ReadLine()) is not null)
        {
            sb.Append(prefix);
            sb.AppendLine(line);
        }
        return sb.ToString();
    }
}
