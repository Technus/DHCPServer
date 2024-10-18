using System.Net;

namespace DHCP.Server.Library.Options;

public class DHCPOptionRequestedIPAddress : DHCPOptionBase
{
    #region IDHCPOption Members

    public IPAddress IPAddress { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionRequestedIPAddress();
        if(s.Length != 4)
            throw new IOException("Invalid DHCP option length");
        result.IPAddress = ParseHelper.ReadIPAddress(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteIPAddress(s, IPAddress);
    }

    #endregion

    public DHCPOptionRequestedIPAddress()
        : base(TDHCPOption.RequestedIPAddress)
    {
        IPAddress = IPAddress.None;
    }

    public DHCPOptionRequestedIPAddress(IPAddress ipAddress)
        : base(TDHCPOption.RequestedIPAddress)
    {
        IPAddress = ipAddress;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{IPAddress}])";
    }
}
