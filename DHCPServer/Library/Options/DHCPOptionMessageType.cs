namespace DHCP.Server.Library.Options;

public class DHCPOptionMessageType : DHCPOptionBase
{
    #region IDHCPOption Members

    public TDHCPMessageType MessageType { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionMessageType();
        if(s.Length != 1)
            throw new IOException("Invalid DHCP option length");
        result.MessageType = (TDHCPMessageType)s.ReadByte();
        return result;
    }

    public override void ToStream(Stream s)
    {
        s.WriteByte((byte)MessageType);
    }

    #endregion

    public DHCPOptionMessageType()
        : base(TDHCPOption.MessageType)
    {
        MessageType = TDHCPMessageType.Undefined;
    }

    public DHCPOptionMessageType(TDHCPMessageType messageType)
        : base(TDHCPOption.MessageType)
    {
        MessageType = messageType;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{MessageType}])";
    }
}
