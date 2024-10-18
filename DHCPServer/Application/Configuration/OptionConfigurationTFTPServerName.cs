using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Service.Configuration;

[Serializable]
public class OptionConfigurationTFTPServerName : OptionConfiguration
{
    public string Name;

    public OptionConfigurationTFTPServerName()
    {
        Name = "";
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionTFTPServerName(Name);
    }
}