using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Service.Configuration;

[Serializable]
public class OptionConfigurationDomainNameServer : OptionConfigurationAddresses
{
    public OptionConfigurationDomainNameServer()
    {
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionDomainNameServer()
        {
            IPAddresses = Addresses
                .Where(x => x.Address != null)
                .Select(x => x.Address).ToList(),
        };
    }
}