namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionMaximumDHCPMessageSize : DHCPOptionBase
{
    #region IDHCPOption Members

    public ushort MaxSize { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionMaximumDHCPMessageSize();
        if(s.Length != 2) 
            throw new IOException("Invalid DHCP option length");
        result.MaxSize = ParseHelper.ReadUInt16(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteUInt16(s, MaxSize);
    }

    #endregion

    public DHCPOptionMaximumDHCPMessageSize()
        : base(TDHCPOption.MaximumDHCPMessageSize)
    {
        MaxSize = default;
    }

    public DHCPOptionMaximumDHCPMessageSize(ushort maxSize)
        : base(TDHCPOption.MaximumDHCPMessageSize)
    {
        MaxSize = maxSize;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{MaxSize}])";
    }
}
