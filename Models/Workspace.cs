using System;
using System.Linq.Expressions;

namespace TogglDoodle.Models
{
    public class Workspace : Model
    {
        public enum Privilege
        {
            Admins,
            Everyone
        }

        public string Name { get; set; }

        public bool IsPremium { get; set; }

        public bool IsAdmin { get; set; }

        public decimal? DefaultHourlyRate { get; set; }

        public string DefaultCurrency { get; set; }

        public Privilege ProjectCreationPrivileges { get; set; }

        public Privilege BillableRatesVisibility { get; set; }

        public int Rounding { get; set; }

        public int RoundingMinutes { get; set; }

        public Uri LogoUrl { get; set; }
        // [Relation(CascadeDelete)]
        public Expression<Func<TimeEntry, bool>> TimeEntries {
            get { return (m) => m.WorkspaceId == Id; }
        }
    }
}
