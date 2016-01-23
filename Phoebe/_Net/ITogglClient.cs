using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Json;

namespace Toggl.Phoebe._Net
{
    public interface ITogglClient
    {
        #region Generic CURD methods

        Task<T> Create<T> (T jsonObject)
        where T : CommonData;

        Task<T> Get<T> (long id)
        where T : CommonData;

        Task<List<T>> List<T> ()
        where T : CommonData;

        Task<T> Update<T> (T jsonObject)
        where T : CommonData;

        Task Delete<T> (T jsonObject)
        where T : CommonData;

        Task Delete<T> (IEnumerable<T> jsonObjects)
        where T : CommonData;

        #endregion

        Task<UserData> GetUser (string username, string password);

        Task<UserData> GetUser (string googleAccessToken);

        Task<List<ClientData>> ListWorkspaceClients (long workspaceId);

        Task<List<ProjectData>> ListWorkspaceProjects (long workspaceId);

        Task<List<WorkspaceUserData>> ListWorkspaceUsers (long workspaceId);

        Task<List<TaskData>> ListWorkspaceTasks (long workspaceId);

        Task<List<TaskData>> ListProjectTasks (long projectId);

        Task<List<ProjectUserData>> ListProjectUsers (long projectId);

        Task<List<TimeEntryData>> ListTimeEntries (DateTime start, DateTime end, CancellationToken cancellationToken);

        Task<List<TimeEntryData>> ListTimeEntries (DateTime start, DateTime end);

        Task<List<TimeEntryData>> ListTimeEntries (DateTime end, int days, CancellationToken cancellationToken);

        Task<List<TimeEntryData>> ListTimeEntries (DateTime end, int days);

        Task<UserRelatedData> GetChanges (DateTime? since);

        Task CreateFeedback (FeedbackJson jsonObject);

        Task CreateExperimentAction (ActionJson jsonObject);
    }
}
