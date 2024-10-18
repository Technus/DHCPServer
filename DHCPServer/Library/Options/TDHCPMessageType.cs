namespace DHCP.Server.Library.Options;

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
