namespace DHCP.Server.Library;

public readonly struct OptionItem(OptionMode mode, IDHCPOption option)
{
    public OptionMode Mode => mode;
    public IDHCPOption Option => option;
}
