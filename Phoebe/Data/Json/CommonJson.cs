using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class CommonJson
    {
        protected CommonJson()
        {
        }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? RemoteId { get; set; }

        [JsonProperty("at")]
        public DateTime ModifiedAt { get; set; }

        [JsonProperty("server_deleted_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? DeletedAt { get; set; }

        internal string ToIdString()
        {
            return String.Concat(GetType().Name, "#", RemoteId.ToString());
        }
    }
}
