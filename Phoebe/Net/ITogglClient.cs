using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Phoebe.Net
{
    public interface ITogglClient
    {
        #region Generic CURD methods

        Task Create<T> (T model)
            where T : Model;

        Task<T> Get<T> (long id)
            where T : Model;

        Task<List<T>> List<T> ()
            where T : Model;

        Task Update<T> (T model)
            where T : Model;

        Task Delete<T> (T model)
            where T : Model;

        Task Delete<T> (IEnumerable<T> models)
            where T : Model;

        #endregion

        Task<UserModel> GetUser (string username, string password);

        Task<UserModel> GetUser (string googleAccessToken);

        Task<List<ClientModel>> ListWorkspaceClients (long workspaceId);

        Task<List<ProjectModel>> ListWorkspaceProjects (long workspaceId);

        Task<List<WorkspaceUserModel>> ListWorkspaceUsers (long workspaceId);

        Task<List<TaskModel>> ListWorkspaceTasks (long workspaceId);

        Task<List<TaskModel>> ListProjectTasks (long projectId);

        Task<List<ProjectUserModel>> ListProjectUsers (long projectId);

        Task<List<TimeEntryModel>> ListTimeEntries (DateTime start, DateTime end);

        Task<List<TimeEntryModel>> ListTimeEntries (DateTime end, int days);

        Task<UserRelatedModels> GetChanges (DateTime? since);
    }
}
