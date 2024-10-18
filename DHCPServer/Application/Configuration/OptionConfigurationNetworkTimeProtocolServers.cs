using GitHub.JPMikkers.DHCP;
using GitHub.JPMikkers.DHCP.Options;

namespace DHCPServerApp
{
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
                    .Select(x => x.Address).ToList(),
            };
        }
    }
}