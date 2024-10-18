using DHCP.Server.Library;
using DHCP.Server.Library.Options;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class OptionConfigurationVendorClassIdentifier : OptionConfiguration
{
    public string DataAsString { get; set; }
    public string DataAsHex {  get; set; }

    public OptionConfigurationVendorClassIdentifier()
    {
        DataAsString = string.Empty;
        DataAsHex = string.Empty;
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        byte[] data;

        if(string.IsNullOrEmpty(DataAsString))
        {
            data = Utils.HexStringToBytes(DataAsHex);
        }
        else
        {
            using var m = new MemoryStream();
            ParseHelper.WriteString(m, DataAsString);
            m.Flush();
            data = m.ToArray();
        }

        return new DHCPOptionVendorClassIdentifier(data);
    }
}