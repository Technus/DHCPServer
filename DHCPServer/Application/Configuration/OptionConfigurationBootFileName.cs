using GitHub.JPMikkers.DHCP;
using GitHub.JPMikkers.DHCP.Options;

namespace DHCPServerApp
{
    [Serializable]
    public class OptionConfigurationBootFileName : OptionConfiguration
    {
        public string Name;

        public OptionConfigurationBootFileName()
        {
            Name = "";
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionBootFileName(Name);
        }
    }
}