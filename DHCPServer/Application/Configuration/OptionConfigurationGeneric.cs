using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Service.Configuration;

[Serializable]
public class OptionConfigurationGeneric : OptionConfiguration
{
    public int Option;
    public string Data;

    public OptionConfigurationGeneric()
    {
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionGeneric((TDHCPOption)Option, Utils.HexStringToBytes(Data));
    }
}