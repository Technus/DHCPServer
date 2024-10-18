using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class OptionConfigurationGeneric : OptionConfiguration
{
    public int Option { get; set; }
    public string Data { get; set; }

    public OptionConfigurationGeneric()
    {
        Data = string.Empty;
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionGeneric((TDHCPOption)Option, Utils.HexStringToBytes(Data));
    }
}