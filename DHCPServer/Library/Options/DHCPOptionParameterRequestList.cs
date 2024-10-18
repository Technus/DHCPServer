using System.Text;

namespace DHCP.Server.Library.Options;

public class DHCPOptionParameterRequestList : DHCPOptionBase
{
    #region IDHCPOption Members

    public List<TDHCPOption> RequestList { get; }

    public override IDHCPOption FromStream(Stream s)
    {
        var result = new DHCPOptionParameterRequestList();
        while(true)
        {
            var c = s.ReadByte();
            if(c < 0)
                break;
            result.RequestList.Add((TDHCPOption)c);
        }
        return result;
    }

    public override void ToStream(Stream s)
    {
        foreach(TDHCPOption opt in RequestList)
        {
            s.WriteByte((byte)opt);
        }
    }

    #endregion

    public DHCPOptionParameterRequestList()
        : base(TDHCPOption.ParameterRequestList)
    {
        RequestList = [];
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach(TDHCPOption opt in RequestList)
        {
            sb.Append(opt.ToString());
            sb.Append(",");
        }
        if(RequestList.Count > 0)
            sb.Remove(sb.Length - 1, 1);
        return $"Option(name=[{OptionType}],value=[{sb}])";
    }
}
