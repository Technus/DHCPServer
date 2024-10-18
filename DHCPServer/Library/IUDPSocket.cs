using System.Net;

namespace DHCP.Server.Library;

public interface IUDPSocket : IDisposable
{
    IPEndPoint LocalEndPoint { get; }

    Task Send(IPEndPoint endPoint, ReadOnlyMemory<byte> msg, CancellationToken cancellationToken);
    Task<(IPEndPoint, ReadOnlyMemory<byte>)> Receive(CancellationToken cancellationToken);
}