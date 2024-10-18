namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionTFTPServerName : DHCPOptionBase
{
    #region IDHCPOption Members

    public string Name { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionTFTPServerName();
        result.Name = ParseHelper.ReadString(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteString(s, Name, ZeroTerminatedStrings);
    }

    #endregion

    public DHCPOptionTFTPServerName()
        : base(TDHCPOption.TFTPServerName)
    {
        Name = string.Empty;
    }

    public DHCPOptionTFTPServerName(string name)
        : base(TDHCPOption.TFTPServerName)
    {
        Name = name;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Name}])";
    }
}
