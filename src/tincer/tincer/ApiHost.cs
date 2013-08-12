using Mono.Zeroconf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;
using tincer.Api.Models;

namespace tincer
{
    public class ApiHost
    {
        static ApiHost()
        {
            Id = Guid.NewGuid();
        }
        public static Guid Id { get; private set; }
        RegisterService serviceRegister = new RegisterService();
        ServiceBrowser sb = new ServiceBrowser();
        void rs_Response(object o, RegisterServiceEventArgs args)
        {
            Console.WriteLine("Registered? {0}, Error Code: {1}", args.IsRegistered ? "Yes" : "No", args.ServiceError);
        }

        void sb_ServiceRemoved(object o, ServiceBrowseEventArgs args)
        {
        }
        public static HostModel[] KnownHosts { get { lock (knownHosts) { return knownHosts.ToArray(); } } }
        static readonly List<HostModel> knownHosts = new List<HostModel>();
        void sb_ServiceAdded(object o, ServiceBrowseEventArgs args)
        {
            args.Service.Resolved += Service_Resolved;
            args.Service.Resolve();
        }

        void Service_Resolved(object o, ServiceResolvedEventArgs args)
        {
            var service = args.Service;
            foreach (IPAddress addr in service.HostEntry.AddressList)
            {
                var host = new HostModel() { Address = addr.ToString(), Port = (ushort)service.Port, Name = service.FullName, Id = new Guid(service.TxtRecord["NODEID"].ValueRaw) };
                Console.WriteLine("Address {0}:{1} resolved for {2} - {3} - ({4})", addr, (ushort)(service.Port), service.HostTarget, service.FullName, host.Id);
                lock (knownHosts)
                {
                    if (!knownHosts.Any(i => i.Address == addr.ToString()) && !IPAddress.IsLoopback(addr))
                    {
                        knownHosts.Add(host);
                    }
                    else
                    {
                        for (int x = 0; x < knownHosts.Count; x++)
                        {
                            if (knownHosts[x].Address == addr.ToString())
                            {
                                host.Probed = knownHosts[x].Id == host.Id && knownHosts[x].Probed;
                                knownHosts[x] = host;
                                break;
                            }
                        }
                    }
                }
            }
        }
        public async void Start()
        {
            if (mServer != null) throw new InvalidOperationException();
            lock (knownHosts) knownHosts.Clear();
            mServer = new HttpSelfHostServer(Configure());
            var serverTask = mServer.OpenAsync();
            RegisterService rs = new RegisterService();
            rs.RegType = "_tincer._tcp";
            rs.ReplyDomain = "local.";
            rs.Name = Environment.MachineName;
            rs.Port = (short)(ushort)Arguments.Global.ListenPort;
            rs.TxtRecord = new TxtRecord();
            rs.TxtRecord.Add("NODEID", ApiHost.Id.ToByteArray());
            rs.Register();
            rs.Response += rs_Response;
            serviceRegister = rs;
            sb.ServiceAdded += sb_ServiceAdded;
            sb.ServiceRemoved += sb_ServiceRemoved;
            sb.Browse("_tincer._tcp", "local.");
            await serverTask;
            timer.Change(30000, 30000);
        }
        Timer timer = new Timer(o => Tick(o), null, Timeout.Infinite, Timeout.Infinite);
        private static void Tick(object o)
        {
            List<Task> UpdateTasks = new List<Task>();
            lock (knownHosts)
            {
               
            }
        }
        public async void Stop()
        {
            if (mServer == null) throw new InvalidOperationException();
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            var closeTask = mServer.CloseAsync();
            mServer = null;
            mConfig = null;
            try
            {
                serviceRegister.Dispose();
            }
            catch { }
            serviceRegister = null;
            try
            {
                sb.Dispose();
            }
            catch { }
            sb = null;
            await closeTask;
            lock (knownHosts) knownHosts.Clear();
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
