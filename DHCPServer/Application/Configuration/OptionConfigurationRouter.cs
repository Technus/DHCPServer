using GitHub.JPMikkers.DHCP;
using GitHub.JPMikkers.DHCP.Options;

namespace DHCPServerApp
{
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
                    .Select(x => x.Address).ToList(),
            };
        }
    }
}