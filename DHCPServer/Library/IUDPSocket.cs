using System.Net;

namespace GitHub.JPMikkers.DHCP;

public interface IUDPSocket : IDisposable
{
    IPEndPoint LocalEndPoint { get; }

    Task Send(IPEndPoint endPoint, ReadOnlyMemory<byte> msg, CancellationToken cancellationToken);
    Task<(IPEndPoint,ReadOnlyMemory<byte>)> Receive(CancellationToken cancellationToken);
}