using System.Xml.Serialization;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class DHCPServerConfigurationList : List<DHCPServerConfiguration>
{
    private static readonly XmlSerializer s_serializer = new(typeof(DHCPServerConfigurationList));

    public static DHCPServerConfigurationList Read(string file)
    {
        DHCPServerConfigurationList result;

        if(File.Exists(file))
        {
            using var s = File.OpenRead(file);
            result = s_serializer.Deserialize(s) as DHCPServerConfigurationList ?? [];
        }
        else
        {
            result = [];
        }

        return result;
    }

    public void Write(string file)
    {
        string? dirName = Path.GetDirectoryName(file);

        if(!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }

        using var s = File.Open(file, FileMode.Create);
        s_serializer.Serialize(s, this);
    }
}