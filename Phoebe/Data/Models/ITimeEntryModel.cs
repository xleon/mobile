using System;
using System.Threading.Tasks;

namespace Toggl.Phoebe.Data.Models
{
    public interface ITimeEntryModel : IModel
    {
        TimeEntryState State { get; set; }

        string Description { get; set; }

        DateTime StartTime { get; set; }

        DateTime? StopTime { get; set; }

        bool IsBillable { get; set; }

        UserModel User { get; set; }

        WorkspaceModel Workspace { get; set; }

        ProjectModel Project { get; set; }

        TaskModel Task { get; set; }

        string GetFormattedDuration ();

        TimeSpan GetDuration ();

        void SetDuration (TimeSpan value);

        Task StartAsync ();

        Task StoreAsync ();

        Task StopAsync ();

        Task<TimeEntryModel> ContinueAsync ();

        Task MapTagsFromModel (TimeEntryModel model);

        Task MapMinorsFromModel (TimeEntryModel model);
    }
}

