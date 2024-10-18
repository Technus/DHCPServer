namespace GitHub.JPMikkers.DHCP.Options;

public class DHCPOptionRelayAgentInformation : DHCPOptionBase
{
    // suboptions found here: http://networksorcery.com/enp/protocol/bootp/option082.htm
    private enum SubOption : byte
    {
        AgentCircuitId = 1,                         // RFC 3046
        AgentRemoteId = 2,                          // RFC 3046
        DOCSISDeviceClass = 4,                      // RFC 3256
        LinkSelection = 5,                          // RFC 3527
        SubscriberId = 6,                           // RFC 3993
        RadiusAttributes = 7,                       // RFC 4014
        Authentication = 8,                         // RFC 4030
        VendorSpecificInformation = 9,              // RFC 4243
        RelayAgentFlags = 10,                       // RFC 5010
        ServerIdentifierOverride = 11,              // RFC 5107
        DHCPv4VirtualSubnetSelection = 151,         // RFC 6607
        DHCPv4VirtualSubnetSelectionControl = 152,  // RFC 6607
    }

    public byte[] Data { get; private set; }

    public byte[] AgentCircuitId { get; private set; }

    public byte[] AgentRemoteId { get; private set; }

    #region IDHCPOption Members

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionRelayAgentInformation();
        result.Data = new byte[s.Length];
        if(s.Read(result.Data, 0, result.Data.Length)!= result.Data.Length)
            throw new IOException();

        // subOptionStream
        using var suStream = new MemoryStream(result.Data);

        while(true)
        {
            int suCode = suStream.ReadByte();
            if(suCode == -1 || suCode == (byte)TDHCPOption.End)//is it even valid???
                break;
            else if(suCode != 0)
            {
                int suLen = suStream.ReadByte();
                if(suLen == -1)
                    break;

                switch((SubOption)suCode)
                {
                    case SubOption.AgentCircuitId:
                        result.AgentCircuitId = new byte[suLen];
                        if(suStream.Read(result.AgentCircuitId, 0, suLen) != suLen)
                            throw new IOException();
                        break;

                    case SubOption.AgentRemoteId:
                        result.AgentRemoteId = new byte[suLen];
                        if(suStream.Read(result.AgentRemoteId, 0, suLen) != suLen)
                            throw new IOException();
                        break;

                    default:
                        suStream.Seek(suLen, SeekOrigin.Current);
                        break;
                }
            }
        }
        return result;
    }

    public override void ToStream(Stream s)
    {
        s.Write(Data, 0, Data.Length);
    }

    #endregion

    public DHCPOptionRelayAgentInformation()
        : base(TDHCPOption.RelayAgentInformation)
    {
        Data = [];
        AgentCircuitId = [];
        AgentRemoteId = [];
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}], value=[AgentCircuitId=[{Utils.BytesToHexString(AgentCircuitId, " ")}], AgentRemoteId=[{Utils.BytesToHexString(AgentCircuitId, " ")}]])";
    }
}
