using DHCP.Server.Library;
using System.Text.RegularExpressions;

namespace DHCP.Server.Service;

public partial class MACTaster
{
    private static readonly Regex s_regex = MyRegex();
    private readonly PrefixItem[] _prefixItems;

    private struct PrefixItem(byte[] prefix, int bits, string id)
    {
        public byte[] Prefix = prefix;
        public int PrefixBits = bits;
        public string Id = id;
    }

    public MACTaster(string configFile)
    {
        try
        {
            var prefixItems =  new List<PrefixItem>();
            using var sr = new StreamReader(configFile);
            string? line;
            while((line = sr.ReadLine()) is not null)
            {
                try
                {
                    var match = s_regex.Match(line);
                    if(match.Success && match.Groups["mac"].Success && match.Groups["id"].Success)
                    {
                        var prefix = Utils.HexStringToBytes(match.Groups["mac"].Value);
                        var id = match.Groups["id"].Value;
                        var prefixBits = prefix.Length * 8;

                        if(match.Groups["netmask"].Success)
                        {
                            prefixBits = int.Parse(match.Groups["netmask"].Value.Substring(1));
                        }

                        prefixItems.Add(new PrefixItem(prefix, prefixBits, id));
                    }
                }
                catch
                {
                    //Ugh...
                }
            }
            _prefixItems = prefixItems.ToArray();
        }
        catch
        {
            //Ughh...
        }
        _prefixItems ??= [];
    }

    private static bool MacMatch(byte[] mac, byte[] prefix, int bits)
    {
        // prefix should have more bits than masklength
        if(((bits + 7) >> 3) > prefix.Length) return false;
        // prefix should be shorter or equal to mac address
        if(prefix.Length > mac.Length) return false;
        for(var t = 0; t < (bits - 7); t += 8)
        {
            if(mac[t >> 3] != prefix[t >> 3]) return false;
        }

        if((bits & 7) > 0)
        {
            var bitMask = (byte)(0xFF00 >> (bits & 7));
            if((mac[bits >> 3] & bitMask) != (prefix[bits >> 3] & bitMask)) return false;
        }
        return true;
    }


    public string Taste(byte[] macAddress)
    {
        for(int i = 0; i < _prefixItems.Length; i++)
        {
            ref var item = ref _prefixItems[i];
            if(MacMatch(macAddress, item.Prefix, item.PrefixBits))
            {
                return item.Id;
            }
        }
        return "Unknown";
    }

    [GeneratedRegex(@"^(?<mac>([0-9a-fA-F][0-9a-fA-F][:\-\.]?)+)(?<netmask>/[0-9]+)?\s*(?<id>\w*)\s*(?<comment>#.*)?", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
