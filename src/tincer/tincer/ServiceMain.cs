using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace tincer
{
    partial class ServiceMain : ServiceBase
    {
        // Service glue code. Just runs ApiHost, which in turn, just sets up and starts the WebAPI server.
        public ServiceMain()
        {
            InitializeComponent();
        }
        private ApiHost mApiHost = new ApiHost();
        protected override void OnStart(string[] args)
        {
            mApiHost.Start();
        }

        protected override void OnStop()
        {
            mApiHost.Stop();
        }
    }
}
