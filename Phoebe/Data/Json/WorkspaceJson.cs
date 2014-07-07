using System;
using Newtonsoft.Json;

namespace Toggl.Phoebe.Data.Json
{
    public class WorkspaceJson : CommonJson
    {
        [JsonProperty ("name")]
        public string Name { get; set; }

        [JsonProperty ("premium")]
        public bool IsPremium { get; set; }

        [JsonProperty ("admin")]
        public bool IsAdmin { get; set; }

        [JsonProperty ("default_hourly_rate", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? DefaultRate { get; set; }

        [JsonProperty ("default_currency")]
        public string DefaultCurrency { get; set; }

        [JsonProperty ("only_admins_may_create_projects")]
        public bool OnlyAdminsMayCreateProjects { get; set; }

        [JsonProperty ("only_admins_see_billable_rates")]
        public bool OnlyAdminsSeeBillableRates { get; set; }

        [JsonProperty ("rounding")]
        public RoundingMode RoundingMode { get; set; }

        [JsonProperty ("rounding_minutes")]
        public int RoundingPercision { get; set; }

        [JsonProperty ("logo_url")]
        public string LogoUrl { get; set; }
    }
}
