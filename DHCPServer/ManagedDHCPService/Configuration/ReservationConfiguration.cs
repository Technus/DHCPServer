using DHCP.Server.Library;
using System.Net;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class ReservationConfiguration
{
    private IPAddress _poolStart = IPAddress.None;
    private IPAddress _poolEnd = IPAddress.None;

    public string MacTaste { get; set; } = string.Empty;

    public string HostName { get; set; } = string.Empty;

    public string PoolStart
    {
        get => _poolStart.ToString();
        set => _poolStart = IPAddress.Parse(value);
    }

    public string PoolEnd
    {
        get => _poolEnd.ToString();
        set => _poolEnd = IPAddress.Parse(value);
    }

    public bool Preempt { get; set; }

    public ReservationItem ConstructReservationItem()
    {
        return new ReservationItem()
        {
            HostName = HostName,
            MacTaste = MacTaste,
            PoolStart = _poolStart,
            PoolEnd = _poolEnd,
            Preempt = Preempt,
        };
    }
}