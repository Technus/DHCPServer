using System.ComponentModel;
using System.Xml.Serialization;

namespace DHCP.Server.Service.Configuration;

[Serializable]
public sealed class DHCPServerConfigurationList : BindingList<DHCPServerConfiguration>
{
    private static readonly XmlSerializer s_serializer = new(typeof(DHCPServerConfigurationList));

    public static DHCPServerConfigurationList Read(string file)
    {
        DHCPServerConfigurationList result;

        if(File.Exists(file))
        {
            using var s = File.OpenRead(file);
            result = (DHCPServerConfigurationList)s_serializer.Deserialize(s);
        }
        else
        {
            result = [];
        }

        return result;
    }

    public void Write(string file)
    {
        string dirName = Path.GetDirectoryName(file);

        if(!Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }

        using var s = File.Open(file, FileMode.Create);
        s_serializer.Serialize(s, this);
    }
}