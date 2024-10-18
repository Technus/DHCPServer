using System.Net;

namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionSubnetMask : DHCPOptionBase
{
    #region IDHCPOption Members

    public IPAddress SubnetMask { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionSubnetMask();
        if(s.Length != 4)
            throw new IOException("Invalid DHCP option length");
        result.SubnetMask = ParseHelper.ReadIPAddress(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteIPAddress(s, SubnetMask);
    }

    #endregion

    public DHCPOptionSubnetMask()
        : base(TDHCPOption.SubnetMask)
    {
        SubnetMask = IPAddress.None;
    }

    public DHCPOptionSubnetMask(IPAddress subnetMask)
        : base(TDHCPOption.SubnetMask)
    {
        SubnetMask = subnetMask;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{SubnetMask}])";
    }
}
