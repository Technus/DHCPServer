using DHCP.Server.Library.Options;
using System.Net;
using System.Xml.Serialization;

namespace DHCP.Server.Library;

[Serializable]
public sealed class DHCPClient : IEquatable<DHCPClient>
{
    public enum TState
    {
        Released,
        Offered,
        Assigned
    }

    private DateTime _offeredTime;
    private DateTime _leaseStartTime;
    private TimeSpan _leaseDuration;

    [XmlElement(DataType = "hexBinary")]
    public byte[] Identifier { get; set; } = [];

    [XmlElement(DataType = "hexBinary")]
    public byte[] HardwareAddress { get; set; } = [];

    public string HostName { get; set; } = string.Empty;

    public TState State { get; set; } = TState.Released;

    [XmlIgnore]
    internal DateTime OfferedTime { get; set; }

    public DateTime LeaseStartTime { get; set; }

    [XmlIgnore]
    public TimeSpan LeaseDuration
    {
        get => _leaseDuration;
        set => _leaseDuration = value.SanitizeTimeSpan();
    }

    public DateTime LeaseEndTime
    {
        get => _leaseDuration.IsInfiniteTimeSpan() ? DateTime.MaxValue : _leaseStartTime + _leaseDuration;
        set => _leaseDuration = value >= DateTime.MaxValue ? Utils.InfiniteTimeSpan : value - _leaseStartTime;
    }

    [XmlIgnore]
    public IPAddress IPAddress { get; set; } = IPAddress.Any;

    [XmlElement(ElementName = "IPAddress")]
    public string IPAddressAsString
    {
        get => IPAddress.ToString();
        set => IPAddress = IPAddress.Parse(value);
    }

    public DHCPClient()
    {
    }

    public DHCPClient Clone()
    {
        var result = new DHCPClient
        {
            Identifier = Identifier,
            HardwareAddress = HardwareAddress,
            HostName = HostName,
            State = State,
            IPAddress = IPAddress,
            _offeredTime = _offeredTime,
            _leaseStartTime = _leaseStartTime,
            _leaseDuration = _leaseDuration
        };
        return result;
    }

    internal static DHCPClient CreateFromMessage(DHCPMessage message)
    {
        var result = new DHCPClient();
        result.HardwareAddress = message.ClientHardwareAddress;

        var dhcpOptionHostName = message.FindOption<DHCPOptionHostName>();

        if(dhcpOptionHostName != null)
        {
            result.HostName = dhcpOptionHostName.HostName;
        }

        var dhcpOptionClientIdentifier = message.FindOption<DHCPOptionClientIdentifier>();

        if(dhcpOptionClientIdentifier != null)
        {
            result.Identifier = dhcpOptionClientIdentifier.Data;
        }
        else
        {
            result.Identifier = message.ClientHardwareAddress;
        }

        return result;
    }

    #region IEquatable and related

    public override bool Equals(object? obj) => Equals(obj as DHCPClient);

    public bool Equals(DHCPClient? other) =>
        other is not null && Utils.ByteArraysAreEqual(Identifier, other.Identifier);

    public override int GetHashCode()
    {
        unchecked
        {
            var result = 0;
            foreach(var b in Identifier)
                result = result * 31 ^ b;
            return result;
        }
    }

    #endregion

    public override string ToString() => $"{Utils.BytesToHexString(Identifier, "-")} ({HostName})";
}
