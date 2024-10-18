using System.Net;

namespace GitHub.JPMikkers.DHCP.Options;

public abstract class DHCPOptionServerListBase : DHCPOptionBase
{
    public IReadOnlyList<IPAddress> IPAddresses { get; set; }

    public abstract DHCPOptionServerListBase Create();

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        if(s.Length % 4 != 0) 
            throw new IOException("Invalid DHCP option length");

        var result = Create();
        var addresses = new List<IPAddress>();
        for(int t = 0; t < s.Length; t += 4)
            addresses.Add(ParseHelper.ReadIPAddress(s));
        result.IPAddresses = addresses;

        return result;
    }

    public override void ToStream(Stream s)
    {
        foreach(var ipAddress in IPAddresses)
            ParseHelper.WriteIPAddress(s, ipAddress);
    }

    #endregion

    protected DHCPOptionServerListBase(TDHCPOption optionType)
        : base(optionType)
    {
        IPAddresses = [];
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{string.Join(",", IPAddresses.Select(x => x.ToString()))}])";
    }
}
