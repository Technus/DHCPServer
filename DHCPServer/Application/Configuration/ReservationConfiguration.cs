using DHCP.Server.Library;
using System.ComponentModel;
using System.Net;

namespace DHCP.Server.Service.Configuration;

[Serializable]
public class ReservationConfiguration
{
    private IPAddress _poolStart;
    private IPAddress _poolEnd;

    public string MacTaste { get; set; }

    public string HostName { get; set; }

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

    [DefaultValue(false)]
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