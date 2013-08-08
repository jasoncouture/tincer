using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace tincer.Discovery.Messages
{
    public class AddressProbeResponse : AddressProbe
    {
        public IPAddress RemoteAddress { get; set; }
        public AddressProbe OriginalProbe { get; set; }
        public AddressProbeResponse(IPEndPoint endPoint, AddressProbe originalProbe)
            : this(endPoint.Address, originalProbe)
        { }
        public AddressProbeResponse(IPAddress remoteAddress, AddressProbe originalProbe)
            : base()
        {
            OriginalProbe = originalProbe;
            RemoteAddress = remoteAddress;
        }
    }
}
