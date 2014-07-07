using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class TimeEntryJson : CommonJson
    {
        [JsonProperty ("description")]
        public string Description { get; set; }

        [JsonProperty ("billable")]
        public bool IsBillable { get; set; }

        [JsonProperty ("start")]
        public DateTime StartTime { get; set; }

        [JsonProperty ("stop", NullValueHandling = NullValueHandling.Include)]
        public DateTime? StopTime { get; set; }

        /// <summary>
        /// Gets or sets the created with. Created with should be automatically set by <see cref="ITogglClient"/>
        /// implementation before sending data to server.
        /// </summary>
        /// <value>The created with string.</value>
        [JsonProperty ("created_with")]
        public string CreatedWith { get; set; }

        [JsonProperty ("duronly")]
        public bool DurationOnly { get; set; }

        [JsonProperty ("duration")]
        public long Duration { get; set; }

        [JsonProperty ("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty ("uid")]
        public long UserId { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceId { get; set; }

        [JsonProperty ("pid")]
        public long? ProjectId { get; set; }

        [JsonProperty ("tid")]
        public long? TaskId { get; set; }
    }
}
