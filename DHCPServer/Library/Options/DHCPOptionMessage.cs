namespace DHCP.Server.Library.Options;

public class DHCPOptionMessage : DHCPOptionBase
{
    #region IDHCPOption Members

    public string Message { get; private set; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionMessage();
        result.Message = ParseHelper.ReadString(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteString(s, Message, ZeroTerminatedStrings);
    }

    #endregion

    public DHCPOptionMessage()
        : base(TDHCPOption.Message)
    {
        Message = string.Empty;
    }

    public DHCPOptionMessage(string message)
        : base(TDHCPOption.Message)
    {
        Message = message;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{Message}])";
    }
}
