using GitHub.JPMikkers.DHCP;
using GitHub.JPMikkers.DHCP.Options;

namespace DHCPServerApp
{
    [Serializable]
    public class OptionConfigurationGeneric : OptionConfiguration
    {
        public int Option;
        public string Data;

        public OptionConfigurationGeneric()
        {
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionGeneric((TDHCPOption)Option, Utils.HexStringToBytes(Data));
        }
    }
}