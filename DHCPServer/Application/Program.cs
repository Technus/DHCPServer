using DHCP.Server.Library;
using System.Configuration.Install;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;

namespace DHCP.Server.Service;

static class Program
{
    public const string CustomEventLog = "DHCPServerLog";
    public const string CustomEventSource = "DHCPServerSource";

    private const string s_switch_Install = "/install";
    private const string s_switch_Uninstall = "/uninstall";
    private const string s_switch_Service = "/service";

    public static string GetConfigurationPath()
    {
        return Path.Combine(Utils.GetSettingsPath(), "Configuration.xml");
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
        processInfo.UseShellExecute = true;
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
            RunElevated(s_switch_Install);
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
            RunElevated(s_switch_Uninstall);
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
        if(args.Length > 0 && args[0].ToLower() == s_switch_Service)
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
#if DEBUG
                    Application.Run(new FormMain(null!));
#endif
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
                    case s_switch_Install:
                        Install();
                        break;

                    case s_switch_Uninstall:
                        Uninstall();
                        break;
                }
            }
        }
    }
}
