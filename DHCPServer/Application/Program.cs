using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;

namespace DHCPServerApp
{
    static class Program
    {
        public const string CustomEventLog = "DHCPServerLog";
        public const string CustomEventSource = "DHCPServerSource";

        private const string Switch_Install = "/install";
        private const string Switch_Uninstall = "/uninstall";
        private const string Switch_Service = "/service";

        public static string GetConfigurationPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JPMikkers\\DHCP Server\\Configuration.xml");
        }

        public static string GetClientInfoPath(string serverName, string serverAddress)
        {
            string configurationPath = GetConfigurationPath();
            return Path.Combine(Path.GetDirectoryName(configurationPath), $"{serverName}_{serverAddress.Replace('.', '_')}.xml");
        }

        public static string GetMacTastePath()
        {
            string configurationPath = GetConfigurationPath();
            return Path.Combine(Path.GetDirectoryName(configurationPath), "mactaste.cfg");
        }

        public static bool HasAdministrativeRight()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool RunElevated(string fileName, string args)
        {
            var processInfo = new ProcessStartInfo();
            processInfo.Verb = "runas";
            processInfo.FileName = fileName;
            processInfo.Arguments = args;
            try
            {
                Process.Start(processInfo);
                return true;
            }
            catch(Exception)
            {
                //Do nothing. Probably the user canceled the UAC window
            }
            return false;
        }

        public static bool RunElevated(string args)
        {
            return RunElevated(Application.ExecutablePath, args);
        }

        private static void Install()
        {
            if(!HasAdministrativeRight())
            {
                RunElevated(Switch_Install);
                return;
            }

            Trace.WriteLine("Installing DHCP service");

            try
            {
                var Installer = new AssemblyInstaller(Assembly.GetExecutingAssembly(), []);
                Installer.UseNewContext = true;
                Installer.Install(null);
                Installer.Commit(null);
            }
            catch(Exception ex)
            {
                Trace.WriteLine($"Exception: {ex}");
            }
        }

        private static void Uninstall()
        {
            if(!HasAdministrativeRight())
            {
                RunElevated(Switch_Uninstall);
                return;
            }

            Trace.WriteLine("Uninstalling DHCP service");

            try
            {
                var Installer = new AssemblyInstaller(Assembly.GetExecutingAssembly(), []);
                Installer.UseNewContext = true;
                Installer.Uninstall(null);
            }
            catch(Exception ex)
            {
                Trace.WriteLine($"Exception: {ex}");
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if(args.Length > 0 && args[0].ToLower() == Switch_Service)
            {
                ServiceBase.Run([new DHCPService()]);
            }
            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                if(args.Length == 0)
                {
                    var serviceController = ServiceController.GetServices()
                        .FirstOrDefault(x => x.ServiceName == "DHCPServer");

                    if(serviceController is null)
                    {
                        if(MessageBox.Show("Service has not been installed yet, install?", "DHCP Server", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Install();
                        }
                    }
                    else
                    {
                        Application.Run(new FormMain(serviceController));
                    }
                }
                else
                {
                    switch(args[0].ToLower())
                    {
                        case Switch_Install:
                            Install();
                            break;

                        case Switch_Uninstall:
                            Uninstall();
                            break;
                    }
                }
            }
        }
    }
}
