namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionClientIdentifier : DHCPOptionBase
{
    public DHCPMessage.THardwareType HardwareType { get; private set; }

    public byte[] Data { get; private set; }

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionClientIdentifier();
        HardwareType = (DHCPMessage.THardwareType)ParseHelper.ReadUInt8(s);
        result.Data = new byte[s.Length - s.Position];
        if(s.Read(result.Data, 0, result.Data.Length) != result.Data.Length)
            throw new IOException();
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteUInt8(s, (byte)HardwareType);
        s.Write(Data, 0, Data.Length);
    }

    #endregion

    public DHCPOptionClientIdentifier()
        : base(TDHCPOption.ClientIdentifier)
    {
        HardwareType = DHCPMessage.THardwareType.Unknown;
        Data = [];
    }

    public DHCPOptionClientIdentifier(DHCPMessage.THardwareType hardwareType, byte[] data)
        : base(TDHCPOption.ClientIdentifier)
    {
        HardwareType = hardwareType;
        Data = data;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],htype=[{HardwareType}],value=[{Utils.BytesToHexString(Data, " ")}])";
    }
}
