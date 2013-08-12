using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.ServiceProcess;
using System.Configuration.Install;
using System.Reflection;
using System.Diagnostics;

namespace tincer
{
    class Program
    {
        public const string ADDRESS = "Address";
        public const string PORT = "Port";
        public const string SERVICE_NAME = "ServiceName";
        static void Main(string[] args)
        {
            if (!Arguments.Parse(args))
            {
                Environment.Exit(100);
            }
            if (!Environment.UserInteractive)
            {
                Arguments.Global.ServiceMode = true;
            }
            if (Arguments.Global.Install)
            {
                Environment.ExitCode = InstallService() ? 0 : 101;
            }
            else if (Arguments.Global.Uninstall)
            {
                Environment.ExitCode = RemoveService() ? 0 : 102;
            }
            else if (Arguments.Global.ServiceMode)
            {
                ServiceBase.Run(new ServiceMain());
            }
            else if (Arguments.Global.Foreground)
            {
                try
                {
                    Console.WriteLine("Starting API Server on http://{0}:{1}/ with id: {2}", Arguments.Global.ListenAddress, Arguments.Global.ListenPort, ApiHost.Id);
                    ApiHost server = new ApiHost();
                    server.Start();
                    Console.WriteLine("Started successfully, press any key to shutdown.");
                    Console.Read();
                    Console.WriteLine("Shutting down server");
                    server.Stop();
                    Console.WriteLine("Server stopped successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("The server failed to start: {0}", ex.Message);
                }
            }
            else
            {
                Console.WriteLine(Arguments.Global.GetUsage());
            }
        }

        private static bool InstallService()
        {
            Console.WriteLine("Installing windows service...");
            Console.WriteLine("API will listen on {0}:{1}", Arguments.Global.ListenAddress, Arguments.Global.ListenPort);
            try
            {
                ManagedInstallerClass.InstallHelper(
                    new[] 
                    { 
                        string.Format("/{0}={1}", ADDRESS, Arguments.Global.ListenAddress), 
                        string.Format("/{0}={1}", PORT, Arguments.Global.ListenPort),
                        string.Format("/{0}={1}", SERVICE_NAME, Arguments.Global.ServiceName),
                        Assembly.GetEntryAssembly().Location 
                    });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Installation failed: {0}", ex.Message);
            }
            return false;
        }

        private static bool RemoveService()
        {
            Console.WriteLine("Removing windows service...");
            try
            {
                ManagedInstallerClass.InstallHelper(
                    new[] 
                    { 
                        "/u", 
                        string.Format("/{0}={1}", SERVICE_NAME, Arguments.Global.ServiceName),
                        Assembly.GetEntryAssembly().Location 
                    });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Removal failed: {0}", ex.Message);
            }
            return false;
        }
    }
}
