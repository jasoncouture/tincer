using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tincer.Discovery.Messages
{
    public class AddressProbe : MessageBase
    {
        public string HostName { get; set; }
        public OperatingSystem OS { get; set; }
        public Guid Id { get; set; }
        public long ApiPort { get; set; }
        private DateTime _created = DateTime.Now;
        public DateTime Created { get { return _created; } }
        public AddressProbe()
        {
            OS = Environment.OSVersion;
            HostName = Environment.MachineName;
            Id = MessageBase.MachineId;
            ApiPort = Arguments.Global.ListenPort;
        }
    }
}
