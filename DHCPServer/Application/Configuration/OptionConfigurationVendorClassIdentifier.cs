﻿using GitHub.JPMikkers.DHCP;
using GitHub.JPMikkers.DHCP.Options;
using System;
using System.IO;

namespace DHCPServerApp
{
    [Serializable]
    public class OptionConfigurationVendorClassIdentifier : OptionConfiguration
    {
        public string DataAsString;
        public string DataAsHex;

        public OptionConfigurationVendorClassIdentifier()
        {
            DataAsString = "";
            DataAsHex = "";
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            byte[] data;

            if(string.IsNullOrEmpty(DataAsString))
            {
                data = Utils.HexStringToBytes(DataAsHex);
            }
            else
            {
                using var m = new MemoryStream();
                ParseHelper.WriteString(m, DataAsString);
                m.Flush();
                data = m.ToArray();
            }

            return new DHCPOptionVendorClassIdentifier(data);
        }
    }
}