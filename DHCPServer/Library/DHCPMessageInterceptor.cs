namespace DHCP.Server.Library;

public interface IDHCPMessageInterceptor
{
    void Apply(DHCPMessage sourceMsg, DHCPMessage targetMsg);
}