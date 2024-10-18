using System.Net;

namespace DHCP.Server.Library;

public interface IUDPSocketFactory
{
    IUDPSocket Create(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl);
}
