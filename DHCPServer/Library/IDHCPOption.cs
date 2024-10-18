using DHCP.Server.Library.Options;

namespace DHCP.Server.Library;

public interface IDHCPOption
{
    bool ZeroTerminatedStrings { get; set; }
    TDHCPOption OptionType { get; }
    IDHCPOption FromStream(Stream s);
    void ToStream(Stream s);
}
