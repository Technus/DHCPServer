namespace DHCP.Server.Library.Options;

public class DHCPOptionRouter : DHCPOptionServerListBase
{
    public override DHCPOptionServerListBase Create()
    {
        return new DHCPOptionRouter();
    }

    public DHCPOptionRouter() : base(TDHCPOption.Router)
    {
    }
}
