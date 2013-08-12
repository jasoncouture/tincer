using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using tincer.Api.Models;

namespace tincer.Api.Controllers
{
    public class NodeController : ApiController
    {
        public IQueryable<HostModel> Get()
        {
            return ApiHost.KnownHosts.AsQueryable();   
        }

        public HostModel Get(Guid Id)
        {
            return Get().FirstOrDefault(i => i.Id == Id);
        }
    }
}
