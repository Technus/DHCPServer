using System.Runtime.Serialization;

namespace DHCP.Server.Library;

[Serializable]
public class UDPSocketException : Exception
{
    public required bool IsFatal { get; init; }

    public UDPSocketException()
    {
    }

    public UDPSocketException(string message) : base(message)
    {
    }

    public UDPSocketException(string message, Exception inner) : base(message, inner)
    {
    }

    [Obsolete("Serializable...")]
    protected UDPSocketException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}