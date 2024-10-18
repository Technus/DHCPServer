using System.Xml.Serialization;

namespace DHCPServerApp
{
    [Serializable]
    public abstract class OptionConfigurationAddresses : OptionConfiguration
    {
        [XmlArrayItem("Address")]
        public List<XmlSerializableIPAddress> Addresses { get; set; }

        protected OptionConfigurationAddresses()
        {
            Addresses = [];
        }
    }
}