using System.Net;

namespace GitHub.JPMikkers.DHCP;

public interface IUDPSocketFactory
{
    IUDPSocket Create(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl);
}
