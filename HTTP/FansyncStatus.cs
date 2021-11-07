using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FanSync.HTTP
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FansyncStatus
    {
        [JsonProperty]
        public int status;

        [JsonProperty]
        public string latest_version;
    }
}
