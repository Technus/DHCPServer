using System.Net;

namespace DHCP.Server.Library;

public class DHCPStopEventArgs : EventArgs
{
    public required Exception? Reason { get; init; }
}

public interface IDHCPServer : IDisposable
{
    event Action<IDHCPServer, DHCPStopEventArgs?>? OnStatusChange;
    event Action<IDHCPServer, string?>? OnTrace;

    IPEndPoint EndPoint { get; set; }
    IPAddress SubnetMask { get; set; }
    IPAddress PoolStart { get; set; }
    IPAddress PoolEnd { get; set; }

    TimeSpan OfferExpirationTime { get; set; }
    TimeSpan LeaseTime { get; set; }
    IList<DHCPClient> Clients { get; }
    string HostName { get; }
    bool Active { get; }
    List<OptionItem> Options { get; set; }
    List<ReservationItem> Reservations { get; set; }

    void Start();
    void Stop();
}