namespace DHCP.Server.Library.Options;

public class DHCPOptionNetworkTimeProtocolServers : DHCPOptionServerListBase
{
    public override DHCPOptionServerListBase Create()
    {
        return new DHCPOptionNetworkTimeProtocolServers();
    }

    public DHCPOptionNetworkTimeProtocolServers() : base(TDHCPOption.NetworkTimeProtocolServers)
    {
    }
}
