using System.Net;

namespace DHCP.Server.Library.Options;

public class DHCPOptionServerIdentifier : DHCPOptionBase
{
    public IPAddress IPAddress { get; private set; }

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionServerIdentifier();
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

    public DHCPOptionServerIdentifier()
        : base(TDHCPOption.ServerIdentifier)
    {
        IPAddress = IPAddress.None;
    }

    public DHCPOptionServerIdentifier(IPAddress ipAddress)
        : base(TDHCPOption.ServerIdentifier)
    {
        IPAddress = ipAddress;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{IPAddress}])";
    }
}
