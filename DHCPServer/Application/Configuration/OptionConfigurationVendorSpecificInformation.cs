using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Service.Configuration;

[Serializable]
public class OptionConfigurationVendorSpecificInformation : OptionConfiguration
{
    public string Information;

    public OptionConfigurationVendorSpecificInformation()
    {
        Information = string.Empty;
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionVendorSpecificInformation(Information);
    }
}