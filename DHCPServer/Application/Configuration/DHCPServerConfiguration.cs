using System.Net;

namespace DHCP.Server.Service.Configuration;

[Serializable]
public sealed class DHCPServerConfiguration
{
    private IPAddress _address;
    private IPAddress _netMask;
    private IPAddress _poolStart;
    private IPAddress _poolEnd;

    public string Name { get; set; }

    public string Address
    {
        get => _address.ToString();
        set => _address = IPAddress.Parse(value);
    }

    public string NetMask
    {
        get => _netMask.ToString();
        set => _netMask = IPAddress.Parse(value);
    }

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

    public int LeaseTime { get; set; }

    public int OfferTime { get; set; }

    public int MinimumPacketSize { get; set; }

    public List<OptionConfiguration> Options { get; set; }

    public List<ReservationConfiguration> Reservations { get; set; }

    public DHCPServerConfiguration()
    {
        Name = "DHCP";
        Address = IPAddress.Loopback.ToString();
        NetMask = "255.255.255.0";
        PoolStart = "0.0.0.0";
        PoolEnd = "255.255.255.255";
        LeaseTime = (int)TimeSpan.FromDays(1.0).TotalSeconds;
        OfferTime = 30;
        MinimumPacketSize = 576;
        Options = [];
        Reservations = [];
    }

    public DHCPServerConfiguration Clone()
    {
        return DeepCopier.Copy(this);
    }
}