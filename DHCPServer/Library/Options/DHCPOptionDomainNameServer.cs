namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionDomainNameServer : DHCPOptionServerListBase
{
    public override DHCPOptionServerListBase Create()
    {
        return new DHCPOptionDomainNameServer();
    }

    public DHCPOptionDomainNameServer() : base(TDHCPOption.DomainNameServer)
    {
    }
}
