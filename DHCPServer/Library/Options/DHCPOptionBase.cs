namespace GitHub.JPMikkers.DHCP.Options;

public abstract class DHCPOptionBase : IDHCPOption
{
    public TDHCPOption OptionType { get; private set; }

    public bool ZeroTerminatedStrings { get; set; }

    public abstract IDHCPOption FromStream(Stream s);
    public abstract void ToStream(Stream s);

    protected DHCPOptionBase(TDHCPOption optionType)
    {
        OptionType = optionType;
    }
}
