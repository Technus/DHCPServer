namespace GitHub.JPMikkers.DHCP.Options;

public enum TDHCPMessageType
{
    Undefined,
    DISCOVER = 1,
    OFFER,
    REQUEST,
    DECLINE,
    ACK,
    NAK,
    RELEASE,
    INFORM,
}
