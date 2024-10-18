using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class OptionConfigurationTFTPServerName : OptionConfiguration
{
    public string Name { get; set; }

    public OptionConfigurationTFTPServerName()
    {
        Name = string.Empty;
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionTFTPServerName(Name);
    }
}