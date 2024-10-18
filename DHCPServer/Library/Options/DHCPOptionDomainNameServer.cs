namespace DHCP.Server.Library.Options;

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
