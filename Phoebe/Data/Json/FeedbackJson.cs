using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    [JsonObject (MemberSerialization.OptIn)]
    public class FeedbackJson
    {
        [JsonProperty ("subject")]
        public string Subject { get; set; }

        [JsonProperty ("details")]
        public string Message { get; set; }

        [JsonProperty ("toggl_version")]
        public string AppVersion { get; set; }

        [JsonProperty ("mobile")]
        public bool IsMobile { get; set; }

        [JsonProperty ("desktop")]
        public bool IsDesktop { get; set; }

        [JsonProperty ("date")]
        public DateTime Timestamp { get; set; }

        [JsonProperty ("attachment")]
        public byte[] AttachmentData { get; set; }

        [JsonProperty ("attachment_name")]
        public string AttachmentName { get; set; }
    }
}

