using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tincer.Discovery.Messages;

namespace tincer.Discovery
{
    public class Beacon : IDisposable
    {
        public const string MULTICAST_ADDRESS = "239.128.64.32";
        public const int MULTICAST_PORT = 5010;
        UdpClient _socket;
        private Timer timer;
        public Beacon()
        {
            _socket = new UdpClient();
            _socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.ExclusiveAddressUse = false;
            _socket.Client.Bind(new IPEndPoint(IPAddress.Any, MULTICAST_PORT));
            _socket.JoinMulticastGroup(IPAddress.Parse(MULTICAST_ADDRESS), 10);
            _socket.BeginReceive(EndReceive, _socket);
            timer = new Timer(o =>
            {
                try { HeartBeat(); }
                catch { }
                if (_socket == null && timer != null)
                {
                    try
                    {
                        timer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    catch { }
                    try
                    {
                        timer.Dispose();
                    }
                    catch { }
                    timer = null;
                }
            }, null, 10000, 120000);
        }

        void EndReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, MULTICAST_PORT);
                var client = (UdpClient)ar.AsyncState;
                var data = client.EndReceive(ar, ref remoteEP);
                MessageBase message = MessageBase.FromBinary(data);
                HandleMessage((dynamic)message, remoteEP);
            }
            catch
            {
            }
            finally
            {
                if (_socket != null)
                {
                    _socket.BeginReceive(EndReceive, _socket);
                }
            }
        }
        void HandleMessage(MessageBase messageBase, IPEndPoint remoteEP)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Unknown packet received, ignoring.");
            }
        }

        void HandleMessage(AddressProbe probe, IPEndPoint remoteEP)
        {
            if (probe.Id != MessageBase.MachineId)
                Send(new AddressProbeResponse(remoteEP, probe));
        }

        void HandleMessage(AddressProbeResponse probe, IPEndPoint remoteEP)
        {
            if (probe.Id != MessageBase.MachineId && probe.OriginalProbe.Id == MessageBase.MachineId)
                this[probe.Id] = probe;
        }

        public void Send(MessageBase message)
        {
            var data = message.ToBinary();
            if (Environment.UserInteractive)
            {
                var json = message.ToJson();
                Console.WriteLine("Sending packet ({0} bytes raw, {1} bytes compressed): {1}", json.Length, json);
                Console.WriteLine("Compressed Size: {0}", data.Length);
            }
            _socket.Client.SendTo(data, new IPEndPoint(IPAddress.Parse(MULTICAST_ADDRESS), MULTICAST_PORT));
            //_socket.Send(data, data.Length, new IPEndPoint(IPAddress.Parse(MULTICAST_ADDRESS), MULTICAST_PORT));
        }
        void IDisposable.Dispose()
        {
            try
            {
                _socket.Close();
            }
            catch { }
            _socket = null;
        }
        public void HeartBeat()
        {
            Send(new AddressProbe());
            Cleanup();
        }
        public AddressProbeResponse[] ActivePeers
        {
            get
            {
                return _probeAnswers.Select(i => i.Value).Where(i => i.Created.AddMinutes(5) > DateTime.Now).ToArray();
            }
        }
        ConcurrentDictionary<Guid, AddressProbeResponse> _probeAnswers = new ConcurrentDictionary<Guid, AddressProbeResponse>();
        private void Cleanup()
        {
            var allPeers = _probeAnswers.Select(i => i.Value).ToList();
            allPeers = allPeers.Where(i => i.Created.AddMinutes(10) < DateTime.Now).ToList();
            AddressProbeResponse trash = null;
            allPeers.ForEach(p =>
            {
                _probeAnswers.TryRemove(p.Id, out trash);
            });
        }
        public AddressProbeResponse this[Guid key]
        {
            get
            {
                AddressProbeResponse ret = null;
                if (_probeAnswers.TryGetValue(key, out ret))
                    return ret;
                return null;
            }
            set
            {
                if (value.Id != key)
                {
                    this[value.Id] = value;
                    return;
                }
                _probeAnswers.AddOrUpdate(key, value, (k, o) =>
                {
                    if (o == null) return value;
                    if (o.Created > value.Created) return o;
                    return value;
                });
            }
        }
    }
}
