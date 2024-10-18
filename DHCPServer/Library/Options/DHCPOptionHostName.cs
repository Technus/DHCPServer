namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionHostName : DHCPOptionBase
{
    #region IDHCPOption Members

    public string HostName { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionHostName();
        result.HostName = ParseHelper.ReadString(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteString(s, HostName, ZeroTerminatedStrings);
    }

    #endregion

    public DHCPOptionHostName()
        : base(TDHCPOption.HostName)
    {
        HostName = string.Empty;
    }

    public DHCPOptionHostName(string hostName)
        : base(TDHCPOption.HostName)
    {
        HostName = hostName;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{HostName}])";
    }
}
