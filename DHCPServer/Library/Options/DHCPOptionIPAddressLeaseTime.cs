namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionIPAddressLeaseTime : DHCPOptionBase
{
    #region IDHCPOption Members

    public TimeSpan LeaseTime { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionIPAddressLeaseTime();
        if(s.Length != 4) 
            throw new IOException("Invalid DHCP option length");
        result.LeaseTime = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteUInt32(s, (uint)LeaseTime.TotalSeconds);
    }

    #endregion

    public DHCPOptionIPAddressLeaseTime()
        : base(TDHCPOption.IPAddressLeaseTime)
    {
        LeaseTime = TimeSpan.Zero;
    }

    public DHCPOptionIPAddressLeaseTime(TimeSpan leaseTime)
        : base(TDHCPOption.IPAddressLeaseTime)
    {
        LeaseTime = leaseTime > Utils.InfiniteTimeSpan ? Utils.InfiniteTimeSpan : leaseTime;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{(LeaseTime == Utils.InfiniteTimeSpan ? "Infinite" : LeaseTime.ToString())}])";
    }
}
