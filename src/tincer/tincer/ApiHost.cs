using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;

namespace tincer
{
    public class ApiHost
    {
        public void Start()
        {
            if (mServer != null) throw new InvalidOperationException();
            mServer = new HttpSelfHostServer(Configure());
            mServer.OpenAsync().Wait();
        }
        public void Stop()
        {
            if (mServer == null) throw new InvalidOperationException();
            mServer.CloseAsync().Wait();
            mServer = null;
            mConfig = null;
        }

        private HttpSelfHostServer mServer = null;
        private HttpSelfHostConfiguration mConfig = null;

        private HttpSelfHostConfiguration Configure()
        {
            var methods = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(i => { try { return i.GetTypes(); } catch { return new Type[0]; } })
                .Select(i => i.GetMethod("App_Configure", BindingFlags.Public | BindingFlags.Static))
                .Where(i => i != null && i.GetParameters().Length == 1 && i.GetParameters()[0].ParameterType == typeof(HttpSelfHostConfiguration))
                .ToList();
            mConfig = new HttpSelfHostConfiguration(string.Format("http://{0}:{1}/", Arguments.Global.ListenAddress, Arguments.Global.ListenPort));
            methods.ForEach(m => m.Invoke(null, new object[] { mConfig }));
            return mConfig;
        }
    }
}
