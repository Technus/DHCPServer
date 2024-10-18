using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class OptionConfigurationNetworkTimeProtocolServers : OptionConfigurationAddresses
{
    public OptionConfigurationNetworkTimeProtocolServers()
    {
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionNetworkTimeProtocolServers()
        {
            IPAddresses = Addresses
                .Where(x => x.Address != null)
                .Select(x => x.Address).ToArray(),
        };
    }
}