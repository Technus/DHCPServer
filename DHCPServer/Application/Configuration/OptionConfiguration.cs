﻿using GitHub.JPMikkers.DHCP;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace DHCPServerApp
{
    [Serializable]
    [XmlInclude(typeof(OptionConfigurationTFTPServerName))]
    [XmlInclude(typeof(OptionConfigurationBootFileName))]
    [XmlInclude(typeof(OptionConfigurationVendorSpecificInformation))]
    [XmlInclude(typeof(OptionConfigurationGeneric))]
    [XmlInclude(typeof(OptionConfigurationVendorClassIdentifier))]
    [XmlInclude(typeof(OptionConfigurationRouter))]
    [XmlInclude(typeof(OptionConfigurationNetworkTimeProtocolServers))]
    [XmlInclude(typeof(OptionConfigurationDomainNameServer))]
    public abstract class OptionConfiguration
    {
        public OptionMode Mode;

        [OptionalField]
        public bool ZeroTerminatedStrings;

        protected OptionConfiguration()
        {
            Mode = OptionMode.Default;
        }

        public OptionItem ConstructOptionItem()
        {
            var option = ConstructDHCPOption();
            option.ZeroTerminatedStrings = ZeroTerminatedStrings;
            return new OptionItem(Mode, option);
        }

        protected abstract IDHCPOption ConstructDHCPOption();
    }
}
