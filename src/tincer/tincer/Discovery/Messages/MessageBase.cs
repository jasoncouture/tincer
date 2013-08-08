using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tincer.Discovery.Messages
{
    public abstract class MessageBase
    {
        public static Guid MachineId { get; private set; }
        public static JsonSerializerSettings JsonSettings { get; private set; }
        static MessageBase()
        {
            MachineId = Guid.NewGuid();
            JsonSettings = new JsonSerializerSettings();
            JsonSettings.TypeNameHandling = TypeNameHandling.All;
            JsonSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
            JsonSettings.TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            JsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            JsonSettings.PreserveReferencesHandling = PreserveReferencesHandling.All;
            JsonSettings.NullValueHandling = NullValueHandling.Ignore;
            JsonSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, JsonSettings);
        }

        public static MessageBase FromJson(string json)
        {
            return JsonConvert.DeserializeObject(json, JsonSettings) as MessageBase;
        }

        public byte[] ToBinary()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzs = new GZipStream(ms, CompressionLevel.Optimal, true))
                {
                    using (StreamWriter sw = new StreamWriter(gzs, Encoding.UTF8))
                    {
                        sw.Write(ToJson());
                        sw.Flush();
                    }
                }
                return ms.ToArray();
            }
        }

        public static MessageBase FromBinary(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (GZipStream gzs = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(gzs, Encoding.UTF8))
                    {
                        return FromJson(sr.ReadToEnd());
                    }
                }
            }
        }
    }
}
