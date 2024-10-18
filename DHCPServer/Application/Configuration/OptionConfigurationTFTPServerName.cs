using GitHub.JPMikkers.DHCP;
using GitHub.JPMikkers.DHCP.Options;

namespace DHCPServerApp
{
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
}