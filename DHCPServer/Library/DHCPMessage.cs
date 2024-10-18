using GitHub.JPMikkers.DHCP.Options;
using System.Net;
using System.Text;

namespace GitHub.JPMikkers.DHCP;

public class DHCPMessage
{
    private static readonly byte[] s_magicNumber = [99, 130, 83, 99];
    private static readonly IDHCPOption[] s_optionsTemplates;

    public enum TOpcode
    {
        Unknown = 0,
        BootRequest = 1,
        BootReply = 2,
    }

    public enum THardwareType
    {
        Unknown = 0,
        Ethernet = 1,
        Experimental_Ethernet = 2,
        Amateur_Radio_AX_25 = 3,
        Proteon_ProNET_Token_Ring = 4,
        Chaos = 5,
        IEEE_802_Networks = 6,
        ARCNET = 7,
        Hyperchannel = 8,
        Lanstar = 9,
        Autonet_Short_Address = 10,
        LocalTalk = 11,
        LocalNet = 12,
        Ultra_link = 13,
        SMDS = 14,
        Frame_Relay = 15,
        Asynchronous_Transmission_Mode1 = 16,
        HDLC = 17,
        Fibre_Channel = 18,
        Asynchronous_Transmission_Mode2 = 19,
        Serial_Line = 20,
        Asynchronous_Transmission_Mode3 = 21,
    };

    /// <summary>
    /// op, Message op code / message type. 1 = BOOTREQUEST, 2 = BOOTREPLY
    /// </summary>
    public TOpcode Opcode { get; set; }

    /// <summary>
    /// htype, Hardware address type, see ARP section in "Assigned Numbers" RFC; e.g., '1' = 10mb ethernet.
    /// </summary>
    public THardwareType HardwareType { get; set; }

    /// <summary>
    /// Client sets to zero, optionally used by relay agents when booting via a relay agent.
    /// </summary>
    public byte Hops { get; set; }

    /// <summary>
    /// xid (Transaction ID), a random number chosen by the client, used by the client and server 
    /// to associate messages and responses between a client and a server.
    /// </summary>
    public uint XID { get; set; }

    /// <summary>
    /// Filled in by client, seconds elapsed since client began address acquisition or renewal process.
    /// </summary>
    public ushort Secs { get; set; }

    /// <summary>
    /// Broadcast flag. From rfc2131: 
    /// If the 'giaddr' field in a DHCP message from a client is non-zero,
    /// the server sends any return messages to the 'DHCP server' port on the
    /// BOOTP relay agent whose address appears in 'giaddr'. If the 'giaddr'
    /// field is zero and the 'ciaddr' field is nonzero, then the server
    /// unicasts DHCPOFFER and DHCPACK messages to the address in 'ciaddr'.
    /// If 'giaddr' is zero and 'ciaddr' is zero, and the broadcast bit is
    /// set, then the server broadcasts DHCPOFFER and DHCPACK messages to
    /// 0xffffffff. If the broadcast bit is not set and 'giaddr' is zero and
    /// 'ciaddr' is zero, then the server unicasts DHCPOFFER and DHCPACK
    /// messages to the client's hardware address and 'yiaddr' address.  In
    /// all cases, when 'giaddr' is zero, the server broadcasts any DHCPNAK
    /// messages to 0xffffffff.
    /// </summary>
    public bool BroadCast { get; set; }

    /// <summary>
    /// ciaddr (Client IP address) only filled in if client is in BOUND, RENEW or REBINDING state and can respond to ARP requests.
    /// </summary>
    public IPAddress ClientIPAddress { get; set; }

    /// <summary>
    /// yiaddr, 'your' (client) IP address.
    /// </summary>
    public IPAddress YourIPAddress { get; set; }

    /// <summary>
    /// siaddr, IP address of next server to use in bootstrap returned in DHCPOFFER, DHCPACK by server.
    /// </summary>
    public IPAddress NextServerIPAddress { get; set; }

    /// <summary>
    /// giaddr (Relay agent IP address) used in booting via a relay agent.
    /// </summary>
    public IPAddress RelayAgentIPAddress { get; set; }

    /// <summary>
    /// chaddr, Client hardware address.
    /// </summary>
    public byte[] ClientHardwareAddress { get; set; }

    /// <summary>
    /// Optional server host name, null terminated string.
    /// </summary>
    public string ServerHostName { get; set; }

    /// <summary>
    /// file (Boot file name) null terminated string; "generic" name or null in 
    /// DHCPDISCOVER, fully qualified directory-path name in DHCPOFFER.
    /// </summary>
    public string BootFileName { get; set; }

    /// <summary>
    /// Optional parameters field.
    /// </summary>
    public List<IDHCPOption> Options { get; }

    /// <summary>
    /// Convenience property to easily get or set the messagetype option
    /// </summary>
    public TDHCPMessageType MessageType
    {
        get
        {
            var messageTypeDHCPOption = FindOption<DHCPOptionMessageType>();
            return messageTypeDHCPOption is not null ? messageTypeDHCPOption.MessageType : TDHCPMessageType.Undefined;
        }
        set
        {
            var currentMessageType = MessageType;
            if(currentMessageType != value)
                Options.Add(new DHCPOptionMessageType(value));
        }
    }

    static DHCPMessage()
    {
        s_optionsTemplates = new IDHCPOption[256];
        for(int t = 1; t < s_optionsTemplates.Length; t++)
            s_optionsTemplates[t] = new DHCPOptionGeneric((TDHCPOption)t);

        void RegisterOption(IDHCPOption option) =>
            s_optionsTemplates[(int)option.OptionType] = option;

        RegisterOption(new DHCPOptionFixedLength(TDHCPOption.Pad));
        RegisterOption(new DHCPOptionFixedLength(TDHCPOption.End));
        RegisterOption(new DHCPOptionHostName());
        RegisterOption(new DHCPOptionIPAddressLeaseTime());
        RegisterOption(new DHCPOptionServerIdentifier());
        RegisterOption(new DHCPOptionRequestedIPAddress());
        RegisterOption(new DHCPOptionOptionOverload());
        RegisterOption(new DHCPOptionTFTPServerName());
        RegisterOption(new DHCPOptionBootFileName());
        RegisterOption(new DHCPOptionMessageType());
        RegisterOption(new DHCPOptionMessage());
        RegisterOption(new DHCPOptionMaximumDHCPMessageSize());
        RegisterOption(new DHCPOptionParameterRequestList());
        RegisterOption(new DHCPOptionRenewalTimeValue());
        RegisterOption(new DHCPOptionRebindingTimeValue());
        RegisterOption(new DHCPOptionVendorClassIdentifier());
        RegisterOption(new DHCPOptionClientIdentifier());
        RegisterOption(new DHCPOptionFullyQualifiedDomainName());
        RegisterOption(new DHCPOptionSubnetMask());
        RegisterOption(new DHCPOptionRouter());
        RegisterOption(new DHCPOptionDomainNameServer());
        RegisterOption(new DHCPOptionNetworkTimeProtocolServers());
#if RELAYAGENTINFORMATION
        RegisterOption(new DHCPOptionRelayAgentInformation());
#endif
    }

    public static DHCPMessage FromMemory(ReadOnlyMemory<byte> s) => new(s);

    public DHCPMessage()
    {
        HardwareType = THardwareType.Ethernet;
        ClientIPAddress = IPAddress.Any;
        YourIPAddress = IPAddress.Any;
        NextServerIPAddress = IPAddress.Any;
        RelayAgentIPAddress = IPAddress.Any;
        ClientHardwareAddress = [];
        ServerHostName = string.Empty;
        BootFileName = string.Empty;
        Options = [];
    }

    private DHCPMessage(ReadOnlyMemory<byte> mem) : this()
    {
        var i = 0;
        var s = mem.Span;
        Opcode = (TOpcode)s[i++];
        HardwareType = (THardwareType)s[i++];
        var macLen = s[i++];
        Hops = s[i++];

        XID = ParseHelper.ReadUInt32(s[i..(i += 4)]);

        Secs = ParseHelper.ReadUInt16(s[i..(i += 2)]);
        BroadCast = (ParseHelper.ReadUInt16(s[i..(i += 2)]) & 0x8000) == 0x8000;

        ClientIPAddress = ParseHelper.ReadIPAddress(s[i..(i += 4)]);
        YourIPAddress = ParseHelper.ReadIPAddress(s[i..(i += 4)]);
        NextServerIPAddress = ParseHelper.ReadIPAddress(s[i..(i += 4)]);
        RelayAgentIPAddress = ParseHelper.ReadIPAddress(s[i..(i += 4)]);

        ClientHardwareAddress = s[i..(i += macLen)].ToArray();
        i += 16 - macLen;

        var serverHostNameBuffer = s[i..(i += 64)].ToArray();
        var bootFileNameBuffer = s[i..(i += 128)].ToArray();

        // read options magic cookie
        if(!s[i..(i += 4)].SequenceEqual(s_magicNumber)) 
            throw new IOException();

        var optionsBuffer = s[i..].ToArray();

        var overload = ScanOverload(optionsBuffer);

        switch(overload)
        {
            default:
                ServerHostName = ParseHelper.ReadZString(serverHostNameBuffer);
                BootFileName = ParseHelper.ReadZString(bootFileNameBuffer);
                Options = ReadOptions(optionsBuffer, [], []);
                break;

            case 1:
                ServerHostName = ParseHelper.ReadZString(serverHostNameBuffer);
                Options = ReadOptions(optionsBuffer, bootFileNameBuffer, []);
                break;

            case 2:
                BootFileName = ParseHelper.ReadZString(bootFileNameBuffer);
                Options = ReadOptions(optionsBuffer, serverHostNameBuffer, []);
                break;

            case 3:
                Options = ReadOptions(optionsBuffer, bootFileNameBuffer, serverHostNameBuffer);
                break;
        }
    }

    public T? FindOption<T>() where T : DHCPOptionBase => 
        Options.OfType<T>().FirstOrDefault();

    public IDHCPOption? GetOption(TDHCPOption optionType) => 
        Options.Find((v) => v.OptionType == optionType);

    public bool IsRequestedParameter(TDHCPOption optionType)
    {
        var dhcpOptionParameterRequestList = FindOption<DHCPOptionParameterRequestList>();
        return (dhcpOptionParameterRequestList is not null && dhcpOptionParameterRequestList.RequestList.Contains(optionType));
    }

    private static void CopyBytes(Stream source, Stream target, int length)
    {
        var buffer = new byte[length];
        if(source.Read(buffer, 0, length) != length)
            throw new IOException();
        target.Write(buffer, 0, length);
    }

    private static List<IDHCPOption> ReadOptions(Span<byte> optionsBuffer, Span<byte> overflow0, Span<byte> overflow1)
    {
        var result = new List<IDHCPOption>();
        ReadOptions(result, optionsBuffer, overflow0, overflow1);
        ReadOptions(result, overflow0, overflow1);
        ReadOptions(result, overflow1);
        return result;
    }

    private static void ReadOptions(List<IDHCPOption> options,
        Span<byte> s,
        Span<byte> spillovers0 = default,
        Span<byte> spillovers1 = default)
    {
        var i = 0;
        while(true)
        {
            if(i >= s.Length)
                break;
            var code = s[i++];
            if(code == (byte)TDHCPOption.End) 
                break;
            else if(code != 0) 
            {
                if(i >= s.Length)
                    break;
                var len = s[i++];

                using var concatStream = new MemoryStream();
                concatStream.Write(s[i..(i += len)]);
                AppendOverflow(code, s, concatStream);
                AppendOverflow(code, spillovers0, concatStream);
                AppendOverflow(code, spillovers1, concatStream);
                concatStream.Position = 0;
                options.Add(s_optionsTemplates[code].FromStream(concatStream));
            }
        }
    }

    private static void AppendOverflow(int code, Span<byte> source, MemoryStream target)
    {
        var i = 0;
        while(true)
        {
            if(i >= source.Length)
                break;
            var c = source[i++];
            if(c == (byte)TDHCPOption.End) 
                break;
            else if(c != 0)
            {
                if(i >= source.Length)
                    break;
                var l = source[i++];

                if(c == code)
                {
                    var startPosition = i - 2;
                    target.Write(source[i..(i += l)]);
                    source[startPosition..i].Clear();
                }
                else
                {
                    i += l;
                }
            }
        }
    }

    /// <summary>
    /// Locate the overload option value in the passed stream.
    /// </summary>
    /// <param name="s"></param>
    /// <returns>Returns the overload option value, or 0 if it wasn't found</returns>
    private static byte ScanOverload(ReadOnlySpan<byte> s)
    {
        var found = false;
        var i = 0;
        byte result = 0;

        while(true)
        {
            if(i >= s.Length)
                break;
            var code = s[i++];
            if(code == (byte)TDHCPOption.End)
                break;
            else if(code == 52)//option overload
            {
                if(s[i++] != 1)
                    throw new IOException("Invalid length of DHCP option 'Option Overload'");
                result = s[i++];
                if(!found)
                    found = true;
                else throw new IOException("'Option Overload' Was specified multiple times");
            }
            else if(code != 0)
            {
                if(i >= s.Length)
                    break;
                var l = s[i++];
                if(l == (byte)TDHCPOption.End)
                    break;
                i += l;
            }
        }
        return result;
    }

    public void ToStream(Stream s, int minimumPacketSize)
    {
        s.WriteByte((byte)Opcode);
        s.WriteByte((byte)HardwareType);
        s.WriteByte((byte)ClientHardwareAddress.Length);
        s.WriteByte((byte)Hops);
        ParseHelper.WriteUInt32(s, XID);
        ParseHelper.WriteUInt16(s, Secs);
        ParseHelper.WriteUInt16(s, BroadCast ? (ushort)0x8000 : (ushort)0x0);
        ParseHelper.WriteIPAddress(s, ClientIPAddress);
        ParseHelper.WriteIPAddress(s, YourIPAddress);
        ParseHelper.WriteIPAddress(s, NextServerIPAddress);
        ParseHelper.WriteIPAddress(s, RelayAgentIPAddress);
        s.Write(ClientHardwareAddress, 0, ClientHardwareAddress.Length);
        for(int t = ClientHardwareAddress.Length; t < 16; t++) s.WriteByte(0);
        ParseHelper.WriteZString(s, ServerHostName, 64);  // BOOTP legacy
        ParseHelper.WriteZString(s, BootFileName, 128);   // BOOTP legacy
        s.Write(s_magicNumber, 0, 4);  // options magic cookie

        // write all options except RelayAgentInformation
        foreach(var option in Options.Where(x => x.OptionType != TDHCPOption.RelayAgentInformation))
        {
            var optionStream = new MemoryStream();
            option.ToStream(optionStream);
            s.WriteByte((byte)option.OptionType);
            s.WriteByte((byte)optionStream.Length);
            optionStream.Position = 0;
            CopyBytes(optionStream, s, (int)optionStream.Length);
        }

#if RELAYAGENTINFORMATION
        // RelayAgentInformation should be the last option before the end according to RFC 3046
        foreach (var option in _options.Where(x => x.OptionType == TDHCPOption.RelayAgentInformation))
        {
            using var optionStream = new MemoryStream();
            option.ToStream(optionStream);
            s.WriteByte((byte)option.OptionType);
            s.WriteByte((byte)optionStream.Length);
            optionStream.Position = 0;
            CopyBytes(optionStream, s, (int)optionStream.Length);
        }
#endif
        // write end option
        s.WriteByte((byte)TDHCPOption.End);
        s.Flush();

        while(s.Length < minimumPacketSize)
            s.WriteByte(0);

        s.Flush();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendFormat(    "Opcode (op)                    : {0}\r\n", Opcode);
        sb.AppendFormat(    "HardwareType (htype)           : {0}\r\n", HardwareType);
        sb.AppendFormat(    "Hops                           : {0}\r\n", Hops);
        sb.AppendFormat(    "XID                            : {0}\r\n", XID);
        sb.AppendFormat(    "Secs                           : {0}\r\n", Secs);
        sb.AppendFormat(    "BroadCast (flags)              : {0}\r\n", BroadCast);
        sb.AppendFormat(    "ClientIPAddress (ciaddr)       : {0}\r\n", ClientIPAddress);
        sb.AppendFormat(    "YourIPAddress (yiaddr)         : {0}\r\n", YourIPAddress);
        sb.AppendFormat(    "NextServerIPAddress (siaddr)   : {0}\r\n", NextServerIPAddress);
        sb.AppendFormat(    "RelayAgentIPAddress (giaddr)   : {0}\r\n", RelayAgentIPAddress);
        sb.AppendFormat(    "ClientHardwareAddress (chaddr) : {0}\r\n", Utils.BytesToHexString(ClientHardwareAddress, "-"));
        sb.AppendFormat(    "ServerHostName (sname)         : {0}\r\n", ServerHostName);
        sb.AppendFormat(    "BootFileName (file)            : {0}\r\n", BootFileName);

        foreach(IDHCPOption option in Options)
            sb.AppendFormat("Option                         : {0}\r\n", option.ToString());

        return sb.ToString();
    }
}
