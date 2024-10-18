using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DHCPServerApp
{
    public partial class FormPickAdapter : Form
    {
        public IPAddress Address { get; private set; } = IPAddress.Loopback;

        public FormPickAdapter()
        {
            InitializeComponent();
            var computerProperties = IPGlobalProperties.GetIPGlobalProperties();//todo why?
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            comboBoxAdapter.DisplayMember = "Description";
            foreach(var adapter in nics)
            {
                comboBoxAdapter.Items.Add(adapter);
            }
            comboBoxAdapter.SelectedIndex = 0;
        }

        private void comboBoxAdapter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBoxAdapter.SelectedIndex >= 0 && comboBoxAdapter.SelectedIndex < comboBoxAdapter.Items.Count)
            {
                comboBoxUnicast.SelectedIndex = -1;
                comboBoxUnicast.Items.Clear();

                try
                {
                    var adapter = (NetworkInterface)comboBoxAdapter.SelectedItem;

                    foreach(var uni in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if(uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            comboBoxUnicast.Items.Add(uni.Address);
                        }
                    }

                    foreach(var uni in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if(uni.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            comboBoxUnicast.Items.Add(uni.Address);
                        }
                    }
                }
                catch
                {
                    //O OK
                }

                if(comboBoxUnicast.Items.Count > 0)
                {
                    comboBoxUnicast.SelectedIndex = 0;
                }
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            if(comboBoxUnicast.SelectedIndex >= 0)
            {
                Address = (IPAddress)comboBoxUnicast.SelectedItem;
            }
        }
    }
}
