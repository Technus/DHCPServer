using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class OptionConfigurationBootFileName : OptionConfiguration
{
    public string Name { get; set; }

    public OptionConfigurationBootFileName()
    {
        Name = string.Empty;
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionBootFileName(Name);
    }
}