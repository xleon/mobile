using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Json;

namespace Toggl.Phoebe.Net
{
    public interface ITogglClient
    {
        #region Generic CURD methods

        Task<T> Create<T> (string authToken, T jsonObject)
        where T : CommonJson;

        Task<T> Get<T> (string authToken, long id)
        where T : CommonJson;

        Task<List<T>> List<T> (string authToken)
        where T : CommonJson;

        Task<T> Update<T> (string authToken, T jsonObject)
        where T : CommonJson;

        Task Delete<T> (string authToken, T jsonObject)
        where T : CommonJson;

        Task Delete<T> (string authToken, IEnumerable<T> jsonObjects)
        where T : CommonJson;

        #endregion

        Task<UserJson> GetUser(string username, string password);

        Task<UserJson> GetUser(string googleAccessToken);

        Task<List<ClientJson>> ListWorkspaceClients(string authToken, long workspaceId);

        Task<List<ProjectJson>> ListWorkspaceProjects(string authToken, long workspaceId);

        Task<List<WorkspaceUserJson>> ListWorkspaceUsers(string authToken, long workspaceId);

        Task<List<TaskJson>> ListWorkspaceTasks(string authToken, long workspaceId);

        Task<List<TaskJson>> ListProjectTasks(string authToken, long projectId);

        Task<List<ProjectUserJson>> ListProjectUsers(string authToken, long projectId);

        Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime start, DateTime end, CancellationToken cancellationToken);

        Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime start, DateTime end);

        Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime end, int days, CancellationToken cancellationToken);

        Task<List<TimeEntryJson>> ListTimeEntries(string authToken, DateTime end, int days);

        Task<UserRelatedJson> GetChanges(string authToken, DateTime? since);

        Task CreateFeedback(string authToken, FeedbackJson jsonObject);

        Task CreateExperimentAction(string authToken, ActionJson jsonObject);
    }
}
