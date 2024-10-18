namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionRebindingTimeValue : DHCPOptionBase
{
    #region IDHCPOption Members

    public TimeSpan TimeSpan { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionRebindingTimeValue();
        if(s.Length != 4) 
            throw new IOException("Invalid DHCP option length");
        result.TimeSpan = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteUInt32(s, (uint)TimeSpan.TotalSeconds);
    }

    #endregion

    public DHCPOptionRebindingTimeValue()
        : base(TDHCPOption.RebindingTimeValue)
    {
        TimeSpan = TimeSpan.Zero;
    }

    public DHCPOptionRebindingTimeValue(TimeSpan timeSpan)
        : base(TDHCPOption.RebindingTimeValue)
    {
        TimeSpan = timeSpan;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{TimeSpan}])";
    }
}
