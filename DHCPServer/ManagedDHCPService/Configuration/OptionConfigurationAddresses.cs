using System.Xml.Serialization;

namespace DHCP.Server.Worker.Configuration;

[Serializable()]
public abstract class OptionConfigurationAddresses : OptionConfiguration
{
    [XmlArrayItem("Address")]
    public List<XmlSerializableIPAddress> Addresses { get; set; }

    public OptionConfigurationAddresses()
    {
        Addresses = [];
    }
}