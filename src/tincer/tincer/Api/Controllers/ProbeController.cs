using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using tincer.Api.Models;

namespace tincer.Api.Controllers
{
    public class ProbeController : ApiController
    {
        public async Task<bool> Post(HostModel host)
        {
            if (host == null) return false;
            return await host.Probe();
        }
        public async Task<bool> Get(Guid? Id)
        {
            if (Id == null) return false;
            NodeController node = new NodeController();
            var host = node.Get(Id.Value);
            return await Post(host);
        }
    }
}
