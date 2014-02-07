using System;
using Newtonsoft.Json;
using Toggl.Phoebe.Bugsnag.Json;
using System.Collections.Generic;

namespace Toggl.Phoebe.Bugsnag.Data
{
    [JsonObject (MemberSerialization.OptIn)]
    public class Event
    {
        [JsonProperty ("user", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public UserInfo User { get; set; }

        [JsonProperty ("app")]
        public ApplicationInfo App { get; set; }

        [JsonProperty ("appState", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ApplicationState AppState { get; set; }

        [JsonProperty ("device", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public SystemInfo System { get; set; }

        [JsonProperty ("deviceState", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public SystemState SystemState { get; set; }

        [JsonProperty ("context")]
        public string Context { get; set; }

        [JsonProperty ("context"), JsonConverter (typeof(ErrorSeverityConverter))]
        public ErrorSeverity Severity { get; set; }

        [JsonProperty ("exceptions")]
        public List<ExceptionInfo> Exceptions { get; set; }

        [JsonProperty ("metadata")]
        public Metadata Metadata { get; set; }
    }
}
