namespace DHCP.Server.Library.Options;

public class DHCPOptionOptionOverload : DHCPOptionBase
{
    #region IDHCPOption Members

    public byte Overload { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionOptionOverload();
        if(s.Length != 1)
            throw new IOException("Invalid DHCP option length");
        result.Overload = (byte)s.ReadByte();
        return result;
    }

    public override void ToStream(Stream s)
    {
        s.WriteByte(Overload);
    }

    #endregion

    public DHCPOptionOptionOverload()
        : base(TDHCPOption.OptionOverload)
    {
        Overload = default;
    }

    public DHCPOptionOptionOverload(byte overload)
        : base(TDHCPOption.OptionOverload)
    {
        Overload = overload;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Overload}])";
    }
}
