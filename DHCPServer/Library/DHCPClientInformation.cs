using System.Xml.Serialization;

namespace GitHub.JPMikkers.DHCP;

[Serializable]
public class DHCPClientInformation
{
    public DateTime TimeStamp { get; } = DateTime.Now;

    public List<DHCPClient> Clients { get; set; } = [];

    private static readonly XmlSerializer s_serializer = new(typeof(DHCPClientInformation));

    public static DHCPClientInformation Read(string file)
    {
        DHCPClientInformation result;

        if(File.Exists(file))
        {
            using var s = File.OpenRead(file);
            result = (s_serializer.Deserialize(s) as DHCPClientInformation) ?? new();
        }
        else
        {
            result = new();
        }

        return result;
    }

    public void Write(string file)
    {
        var dirName = Path.GetDirectoryName(file);

        if(!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }

        using var s = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        s_serializer.Serialize(s, this);
        s.Flush();
    }
}
