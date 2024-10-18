namespace DHCP.Server.Library.Options;

public class DHCPOptionFullyQualifiedDomainName : DHCPOptionBase
{
    public byte[] Data { get; private set; }

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionFullyQualifiedDomainName();
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

    public DHCPOptionFullyQualifiedDomainName()
        : base(TDHCPOption.FullyQualifiedDomainName)
    {
        Data = [];
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Utils.BytesToHexString(Data, " ")}])";
    }
}
