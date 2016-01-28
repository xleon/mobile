using SQLite.Net.Attributes;

namespace Toggl.Phoebe._Data.Models
{
    [Table ("WorkspaceModel")]
    public class WorkspaceData : CommonData
    {
        public WorkspaceData ()
        {
            ProjectCreationPrivileges = AccessLevel.Any;
            BillableRatesVisibility = AccessLevel.Any;
        }

        public WorkspaceData (WorkspaceData other) : base (other)
        {
            Name = other.Name;
            IsPremium = other.IsPremium;
            DefaultRate = other.DefaultRate;
            DefaultCurrency = other.DefaultCurrency;
            ProjectCreationPrivileges = other.ProjectCreationPrivileges;
            BillableRatesVisibility = other.BillableRatesVisibility;
            RoundingMode = other.RoundingMode;
            RoundingPercision = other.RoundingPercision;
            LogoUrl = other.LogoUrl;
            IsAdmin = other.IsAdmin;
        }

        public string Name { get; set; }

        public bool IsPremium { get; set; }

        public decimal? DefaultRate { get; set; }

        public string DefaultCurrency { get; set; }

        public AccessLevel ProjectCreationPrivileges { get; set; }

        public AccessLevel BillableRatesVisibility { get; set; }

        public bool OnlyAdminsMayCreateProjects { get; set; }

        public bool OnlyAdminsSeeBillableRates { get; set; }

        public RoundingMode RoundingMode { get; set; }

        public int RoundingPercision { get; set; }

        public string LogoUrl { get; set; }

        public bool IsAdmin { get; set; }
    }
}
