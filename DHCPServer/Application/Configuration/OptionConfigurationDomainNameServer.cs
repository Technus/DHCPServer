﻿using GitHub.JPMikkers.DHCP;
using GitHub.JPMikkers.DHCP.Options;

namespace DHCPServerApp
{
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
}