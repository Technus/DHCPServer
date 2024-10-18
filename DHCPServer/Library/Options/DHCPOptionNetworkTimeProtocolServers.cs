namespace GitHub.JPMikkers.DHCP.Options;

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
