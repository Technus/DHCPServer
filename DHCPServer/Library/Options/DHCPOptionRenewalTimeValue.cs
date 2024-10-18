namespace DHCP.Server.Library.Options;

public class DHCPOptionRenewalTimeValue : DHCPOptionBase
{
    #region IDHCPOption Members

    public TimeSpan TimeSpan { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionRenewalTimeValue();
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

    public DHCPOptionRenewalTimeValue()
        : base(TDHCPOption.RenewalTimeValue)
    {
        TimeSpan = TimeSpan.Zero;
    }

    public DHCPOptionRenewalTimeValue(TimeSpan timeSpan)
        : base(TDHCPOption.RenewalTimeValue)
    {
        TimeSpan = timeSpan;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{TimeSpan}])";
    }
}
