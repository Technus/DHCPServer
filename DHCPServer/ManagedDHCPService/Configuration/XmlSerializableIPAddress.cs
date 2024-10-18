﻿using System.Net;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DHCP.Server.Worker.Configuration;

[Serializable]
public class XmlSerializableIPAddress : IXmlSerializable
{
    public IPAddress Address { get; set; }

    public XmlSerializableIPAddress()
    {
        Address = IPAddress.None;
    }

    public XmlSchema? GetSchema()
    {
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        // https://www.codeproject.com/Articles/43237/How-to-Implement-IXmlSerializable-Correctly
        reader.MoveToContent();
        var isEmptyElement = reader.IsEmptyElement;
        reader.ReadStartElement();
        if(!isEmptyElement)
        {
            Address = IPAddress.Parse(reader.ReadContentAsString());
            reader.ReadEndElement();
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        if(Address != null) writer.WriteString(Address.ToString());
    }
}