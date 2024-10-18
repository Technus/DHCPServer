using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Runtime.InteropServices;

namespace DHCP.Server.Library;

public class DefaultUDPSocketFactory : IUDPSocketFactory
{
    private readonly ILogger _logger;

    public DefaultUDPSocketFactory(ILogger? logger)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public IUDPSocket Create(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl)
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger.LogInformation("creating UDPSocket of type {SocketImpl}", nameof(UDPSocketLinux));
            return new UDPSocketLinux(localEndPoint, packetSize, dontFragment, ttl);
        }
        else
        {
            _logger.LogInformation("creating UDPSocket of type {SocketImpl}", nameof(UDPSocketWindows));
            return new UDPSocketWindows(localEndPoint, packetSize, dontFragment, ttl);
        }
    }
}
