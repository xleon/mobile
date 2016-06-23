using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.Models
{
    [Flags]
    public enum AccessLevel
    {
        Regular = 1 << 0,
        Admin = 1 << 1,
        Any = Regular | Admin,
    }

    public interface IWorkspaceData : ICommonData
    {
        string Name { get; }
        bool IsPremium { get; }
        decimal? DefaultRate { get; }
        string DefaultCurrency { get; }
        AccessLevel ProjectCreationPrivileges { get; }
        AccessLevel BillableRatesVisibility { get; }
        bool OnlyAdminsMayCreateProjects { get; }
        bool OnlyAdminsSeeBillableRates { get; }
        RoundingMode RoundingMode { get; }
        int RoundingPrecision { get; }
        string LogoUrl { get; }
        bool IsAdmin { get; }
        IWorkspaceData With(Action<WorkspaceData> transform);
        bool ProjectsBillableByDefault { get; }
    }

    [Table("WorkspaceModel")]
    public class WorkspaceData : CommonData, IWorkspaceData
    {
        public static IWorkspaceData Create(Action<WorkspaceData> transform = null)
        {
            return CommonData.Create(transform);
        }

        /// <summary>
        /// ATTENTION: This constructor should only be used by SQL and JSON serializers
        /// To create new objects, use the static Create method instead
        /// </summary>
        public WorkspaceData()
        {
            ProjectCreationPrivileges = AccessLevel.Any;
            BillableRatesVisibility = AccessLevel.Any;
        }

        WorkspaceData(WorkspaceData other) : base(other)
        {
            Name = other.Name;
            IsPremium = other.IsPremium;
            DefaultRate = other.DefaultRate;
            DefaultCurrency = other.DefaultCurrency;
            ProjectCreationPrivileges = other.ProjectCreationPrivileges;
            BillableRatesVisibility = other.BillableRatesVisibility;
            RoundingMode = other.RoundingMode;
            RoundingPrecision = other.RoundingPrecision;
            LogoUrl = other.LogoUrl;
            IsAdmin = other.IsAdmin;
            ProjectsBillableByDefault = other.ProjectsBillableByDefault;
        }

        public override object Clone()
        {
            return new WorkspaceData(this);
        }

        public IWorkspaceData With(Action<WorkspaceData> transform)
        {
            return base.With(transform);
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

        public int RoundingPrecision { get; set; }

        public string LogoUrl { get; set; }

        public bool IsAdmin { get; set; }

        // [Ignore]
        public bool ProjectsBillableByDefault { get; set; }
    }
}
