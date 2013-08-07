using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace tincer
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        private string _Parameters = null;
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void ProjectInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {
            Prepare();
        }

        private void Prepare()
        {
            // Load any installer parameters into the Arguments singleton then setup the service installer.
            if (Arguments.Global == null) Arguments.Global = new Arguments()
            {
                ServiceName = "tincer",
                ListenAddress = IPAddress.Any
            };

            if (Context.Parameters.ContainsKey(Program.SERVICE_NAME))
                Arguments.Global.ServiceName = Context.Parameters[Program.SERVICE_NAME];
            if (Context.Parameters.ContainsKey(Program.ADDRESS))
                Arguments.Global.ListenAddress = IPAddress.Parse(Context.Parameters[Program.ADDRESS]);
            if (Context.Parameters.ContainsKey(Program.PORT))
                Arguments.Global.ListenPort = int.Parse(Context.Parameters[Program.PORT]);
            SetupInstaller();
        }
        private void SetupInstaller()
        {
            serviceInstaller1.DelayedAutoStart = true;
            serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            serviceInstaller1.DisplayName = string.Format("Tinc Configuration Service (tincer - {0})", Arguments.Global.ServiceName);
            serviceInstaller1.ServiceName = Arguments.Global.ServiceName;
            serviceInstaller1.Description = string.Format("Tincer tinc configuration service ({0}), maintains and updates the tinc configuration as needed to dynamically configure a mesh VPN", Arguments.Global.ServiceName);
            _Parameters = string.Format("--service --{0}={1} --{2}={3} --{4}={5}", Program.ADDRESS, Arguments.Global.ListenAddress, Program.PORT, Arguments.Global.ListenPort, Program.SERVICE_NAME, Arguments.Global.ServiceName);
            Debug.WriteLine(_Parameters);
        }
        private void ProjectInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            Prepare();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            // Why the fuck isn't this in the installer API?
            IntPtr hScm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (hScm == IntPtr.Zero)
                throw new Win32Exception();
            try
            {
                IntPtr hSvc = OpenService(hScm, serviceInstaller1.ServiceName, SERVICE_ALL_ACCESS);
                if (hSvc == IntPtr.Zero)
                    throw new Win32Exception();
                try
                {
                    QUERY_SERVICE_CONFIG oldConfig;
                    uint bytesAllocated = 8192; // Per documentation, 8K is max size.
                    IntPtr ptr = Marshal.AllocHGlobal((int)bytesAllocated);
                    try
                    {
                        uint bytesNeeded;
                        if (!QueryServiceConfig(hSvc, ptr, bytesAllocated, out bytesNeeded))
                        {
                            throw new Win32Exception();
                        }
                        oldConfig = (QUERY_SERVICE_CONFIG)Marshal.PtrToStructure(ptr, typeof(QUERY_SERVICE_CONFIG));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }

                    string newBinaryPathAndParameters = oldConfig.lpBinaryPathName + " " + _Parameters;

                    if (!ChangeServiceConfig(hSvc, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, SERVICE_NO_CHANGE,
                        newBinaryPathAndParameters, null, IntPtr.Zero, null, null, null, null))
                        throw new Win32Exception();
                }
                finally
                {
                    if (!CloseServiceHandle(hSvc))
                        throw new Win32Exception();
                }
            }
            finally
            {
                if (!CloseServiceHandle(hScm))
                    throw new Win32Exception();
            }
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr OpenSCManager(
            string lpMachineName,
            string lpDatabaseName,
            uint dwDesiredAccess);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr OpenService(
            IntPtr hSCManager,
            string lpServiceName,
            uint dwDesiredAccess);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct QUERY_SERVICE_CONFIG
        {
            public uint dwServiceType;
            public uint dwStartType;
            public uint dwErrorControl;
            public string lpBinaryPathName;
            public string lpLoadOrderGroup;
            public uint dwTagId;
            public string lpDependencies;
            public string lpServiceStartName;
            public string lpDisplayName;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryServiceConfig(
            IntPtr hService,
            IntPtr lpServiceConfig,
            uint cbBufSize,
            out uint pcbBytesNeeded);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeServiceConfig(
            IntPtr hService,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword,
            string lpDisplayName);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(
            IntPtr hSCObject);

        private const uint SERVICE_NO_CHANGE = 0xffffffffu;
        private const uint SC_MANAGER_ALL_ACCESS = 0xf003fu;
        private const uint SERVICE_ALL_ACCESS = 0xf01ffu;

    }
}
