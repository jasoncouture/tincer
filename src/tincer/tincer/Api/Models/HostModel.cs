using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace tincer.Api.Models
{
    public class HostModel
    {
        public HostModel() { LastUpdate = DateTime.Now; }
        public DateTime LastUpdate { get; set; }
        public Guid Id { get; set; }
        public bool Probed { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public int Port { get; set; }

        public async Task<bool> Probe()
        {
            HttpClient client = new HttpClient();
            string url = string.Format("http://{0}:{1}/api/node", this.Address, this.Port);
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode) return true;
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
