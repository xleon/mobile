using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class UserInfo
    {
        [JsonProperty ("id")]
        public string Id { get; set; }

        [JsonProperty ("email")]
        public string Email { get; set; }

        [JsonProperty ("name")]
        public string Name { get; set; }
    }
}

