using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Toggl.Phoebe._Data.Json
{
    public class TimeEntryJson : CommonJson
    {
        [DefaultValue ("")]
        [JsonProperty (PropertyName = "description", DefaultValueHandling = DefaultValueHandling.Populate)]
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
        public long UserRemoteId { get; set; }

        [JsonProperty ("wid")]
        public long WorkspaceRemoteId { get; set; }

        [JsonProperty ("pid")]
        public long? ProjectRemoteId { get; set; }

        [JsonProperty ("tid")]
        public long? TaskRemoteId { get; set; }
    }
}
