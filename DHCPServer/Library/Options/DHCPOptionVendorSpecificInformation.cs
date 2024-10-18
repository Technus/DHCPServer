namespace DHCP.Server.Library.Options;

public class DHCPOptionVendorSpecificInformation : DHCPOptionBase
{
    public byte[] Data { get; set; }

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionVendorSpecificInformation();
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

    public DHCPOptionVendorSpecificInformation()
        : base(TDHCPOption.VendorSpecificInformation)
    {
        Data = [];
    }

    public DHCPOptionVendorSpecificInformation(byte[] data)
        : base(TDHCPOption.VendorSpecificInformation)
    {
        Data = data;
    }

    public DHCPOptionVendorSpecificInformation(string data)
        : base(TDHCPOption.VendorSpecificInformation)
    {
        using var ms = new MemoryStream();
        ParseHelper.WriteString(ms, data, ZeroTerminatedStrings);
        ms.Flush();
        Data = ms.ToArray();
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Utils.BytesToHexString(Data, " ")}])";
    }
}
