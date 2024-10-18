namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionVendorClassIdentifier : DHCPOptionBase
{
    public byte[] Data { get; set; }

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionVendorClassIdentifier();
        result.Data = new byte[s.Length];
        if(s.Read(result.Data, 0, result.Data.Length) != result.Data.Length)
            throw new IOException();
        return result;
    }

    public override void ToStream(Stream s)
    {
        s.Write(Data, 0, Data.Length);
    }

    #endregion

    public DHCPOptionVendorClassIdentifier()
        : base(TDHCPOption.VendorClassIdentifier)
    {
        Data = [];
    }

    public DHCPOptionVendorClassIdentifier(byte[] data)
        : base(TDHCPOption.VendorClassIdentifier)
    {
        Data = data;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Utils.BytesToHexString(Data, " ")}])";
    }
}
