using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class Notification
    {
        [JsonProperty ("apiKey")]
        public string ApiKey { get; set; }

        [JsonProperty ("notifier")]
        public NotifierInfo Notifier { get; set; }

        [JsonProperty ("events")]
        public List<Event> Events { get; set; }
    }
}
