namespace DHCP.Server.Library.Options;

public class DHCPOptionBootFileName : DHCPOptionBase
{
    #region IDHCPOption Members

    public string Name { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionBootFileName();
        result.Name = ParseHelper.ReadString(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteString(s, Name, ZeroTerminatedStrings);
    }

    #endregion

    public DHCPOptionBootFileName()
        : base(TDHCPOption.BootFileName)
    {
        Name = string.Empty;
    }

    public DHCPOptionBootFileName(string name)
        : base(TDHCPOption.BootFileName)
    {
        Name = name;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Name}])";
    }
}
