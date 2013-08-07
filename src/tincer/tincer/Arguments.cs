using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using System.Net;

namespace tincer
{
    public class Arguments
    {
        [Option('i', "install", HelpText = "Install the windows service, using the specified configuration", DefaultValue = false, Required = false, MutuallyExclusiveSet = "1")]
        public bool Install { get; set; }
        [Option('u', "uninstall", HelpText = "Remove the windows service", DefaultValue = false, Required = false, MutuallyExclusiveSet = "1")]
        public bool Uninstall { get; set; }
        [Option('n', Program.SERVICE_NAME, HelpText = "Name of the windows service to install or remove", DefaultValue = "tincer", Required = false, MutuallyExclusiveSet = "0")]
        public string ServiceName { get; set; }
        [Option('s', "service", HelpText = "Run in windows service mode", DefaultValue = false, Required = false, MutuallyExclusiveSet = "0")]
        public bool ServiceMode { get; set; }
        [Option('f', "foreground", HelpText = "Run in the foreground", DefaultValue = false, Required = false, MutuallyExclusiveSet = "0")]
        public bool Foreground { get; set; }
        [Option(Program.ADDRESS, HelpText = "Listen address", DefaultValue = "0.0.0.0", Required = false)]
        public string _ListenAddress { get; set; }
        public IPAddress ListenAddress { get { return IPAddress.Parse(_ListenAddress); } set { _ListenAddress = (value ?? IPAddress.Any).ToString(); } }
        [Option(Program.PORT, HelpText = "Port number to listen on, Defaults to 8921", DefaultValue = 8921, Required = false)]
        public int ListenPort { get; set; }
        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
        public static Arguments Global { get; internal set; }

        public static bool Parse(string[] args)
        {
            var argsObject = new Arguments();
            bool ret = false;
            if (ret = CommandLine.Parser.Default.ParseArguments(args, argsObject))
            {
                Global = argsObject;
                Global.ListenAddress = Global.ListenAddress ?? IPAddress.Any;
                Global.ServiceName = string.IsNullOrWhiteSpace(Global.ServiceName) ? "tincer" : Global.ServiceName;
            }
            return ret;
        }
    }
}
