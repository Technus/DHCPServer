using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class OptionConfigurationRouter : OptionConfigurationAddresses
{
    public OptionConfigurationRouter()
    {
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionRouter()
        {
            IPAddresses = Addresses
                .Where(x => x.Address != null)
                .Select(x => x.Address).ToArray(),
        };
    }
}