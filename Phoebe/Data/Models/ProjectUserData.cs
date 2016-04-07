using System;
using SQLite.Net.Attributes;

namespace Toggl.Phoebe.Data.Models
{
    public interface IProjectUserData : ICommonData
    {
        bool IsManager { get; }
        int HourlyRate { get; }
        long UserRemoteId { get; }
        long ProjectRemoteId { get; }
        Guid ProjectId { get; }
        Guid UserId { get; }
        IProjectUserData With(Action<ProjectUserData> transform);
    }

    [Table("ProjectUserModel")]
    public class ProjectUserData : CommonData, IProjectUserData
    {
        public static IProjectUserData Create(Action<ProjectUserData> transform = null)
        {
            return CommonData.Create(transform);
        }

        /// <summary>
        /// ATTENTION: This constructor should only be used by SQL and JSON serializers
        /// To create new objects, use the static Create method instead
        /// </summary>
        public ProjectUserData()
        {
        }

        public ProjectUserData(ProjectUserData other) : base(other)
        {
            IsManager = other.IsManager;
            HourlyRate = other.HourlyRate;
            ProjectId = other.ProjectId;
            UserId = other.UserId;
            UserRemoteId = other.UserRemoteId;
            ProjectRemoteId = other.ProjectRemoteId;
        }

        public override object Clone()
        {
            return new ProjectUserData(this);
        }

        public IProjectUserData With(Action<ProjectUserData> transform)
        {
            return base.With(transform);
        }

        public bool IsManager { get; set; }

        public int HourlyRate { get; set; }

        public long UserRemoteId { get; set; }

        public long ProjectRemoteId { get; set; }

        public Guid ProjectId { get; set; }

        public Guid UserId { get; set; }
    }
}
