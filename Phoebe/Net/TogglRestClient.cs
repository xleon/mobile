using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data;

namespace Toggl.Phoebe.Net
{
    public class TogglRestClient : ITogglClient
    {
        private readonly Uri v8Url;
        private readonly Dictionary<Type, Uri> modelUrls;

        public TogglRestClient (Uri url)
        {
            v8Url = new Uri (url, "v8");
            modelUrls = new Dictionary<Type, Uri> () {
                { typeof(ClientModel), new Uri (v8Url, "clients") },
                { typeof(ProjectModel), new Uri (v8Url, "projects") },
                { typeof(TaskModel), new Uri (v8Url, "tasks") },
                { typeof(TimeEntryModel), new Uri (v8Url, "time_entries") },
                { typeof(WorkspaceModel), new Uri (v8Url, "workspaces") },
            };
        }

        public async Task Create<T> (T model)
            where T : Model
        {
            if (typeof(T) == typeof(ClientModel)) {
                await CreateClient (model as ClientModel);
            } else if (typeof(T) == typeof(ProjectModel)) {
                await CreateProject (model as ProjectModel);
            } else if (typeof(T) == typeof(TaskModel)) {
                await CreateTask (model as TaskModel);
            } else if (typeof(T) == typeof(TimeEntryModel)) {
                await CreateTimeEntry (model as TimeEntryModel);
            } else if (typeof(T) == typeof(WorkspaceModel)) {
                await CreateWorkspace (model as WorkspaceModel);
            } else if (typeof(T) == typeof(UserModel)) {
                await CreateUser (model as UserModel);
            } else {
                throw new NotSupportedException ("Creating of model (of type T) is not supported.");
            }
        }

        public async Task<T> Get<T> (long id)
            where T : Model
        {
            if (typeof(T) == typeof(ClientModel)) {
                return await GetClient (id) as T;
            } else if (typeof(T) == typeof(ProjectModel)) {
                return await GetProject (id) as T;
            } else if (typeof(T) == typeof(TaskModel)) {
                return await GetTask (id) as T;
            } else if (typeof(T) == typeof(TimeEntryModel)) {
                return await GetTimeEntry (id) as T;
            } else if (typeof(T) == typeof(WorkspaceModel)) {
                return await GetWorkspace (id) as T;
            } else if (typeof(T) == typeof(UserModel)) {
                return await GetUser (id) as T;
            } else {
                throw new NotSupportedException ("Fetching of model (of type T) is not supported.");
            }
        }

        public async Task<List<T>> List<T> ()
            where T : Model
        {
            if (typeof(T) == typeof(ClientModel)) {
                return await ListClients () as List<T>;
            } else if (typeof(T) == typeof(ProjectModel)) {
                return await ListProjects () as List<T>;
            } else if (typeof(T) == typeof(TaskModel)) {
                return await ListTasks () as List<T>;
            } else if (typeof(T) == typeof(TimeEntryModel)) {
                return await ListTimeEntries () as List<T>;
            } else if (typeof(T) == typeof(WorkspaceModel)) {
                return await ListWorkspaces () as List<T>;
            } else {
                throw new NotSupportedException ("Listing of models (of type T) is not supported.");
            }
        }

        public async Task Update<T> (T model)
            where T : Model
        {
            if (typeof(T) == typeof(ClientModel)) {
                await UpdateClient (model as ClientModel);
            } else if (typeof(T) == typeof(ProjectModel)) {
                await UpdateProject (model as ProjectModel);
            } else if (typeof(T) == typeof(TaskModel)) {
                await UpdateTask (model as TaskModel);
            } else if (typeof(T) == typeof(TimeEntryModel)) {
                await UpdateTimeEntry (model as TimeEntryModel);
            } else if (typeof(T) == typeof(WorkspaceModel)) {
                await UpdateWorkspace (model as WorkspaceModel);
            } else if (typeof(T) == typeof(UserModel)) {
                await UpdateUser (model as UserModel);
            } else {
                throw new NotSupportedException ("Updating of model (of type T) is not supported.");
            }
        }

        public async Task Delete<T> (T model)
            where T : Model
        {
            if (typeof(T) == typeof(ClientModel)) {
                await DeleteClient (model as ClientModel);
            } else if (typeof(T) == typeof(ProjectModel)) {
                await DeleteProject (model as ProjectModel);
            } else if (typeof(T) == typeof(TaskModel)) {
                await DeleteTask (model as TaskModel);
            } else if (typeof(T) == typeof(TimeEntryModel)) {
                await DeleteTimeEntry (model as TimeEntryModel);
            } else {
                throw new NotSupportedException ("Deleting of model (of type T) is not supported.");
            }
        }

        public async Task Delete<T> (IEnumerable<T> models)
            where T : Model
        {
            if (typeof(T) == typeof(ClientModel)) {
                await Task.WhenAll (models.Select ((model) => DeleteClient (model as ClientModel)));
            } else if (typeof(T) == typeof(ProjectModel)) {
                await DeleteProjects (models as IEnumerable<ProjectModel>);
            } else if (typeof(T) == typeof(TaskModel)) {
                await DeleteTasks (models as IEnumerable<TaskModel>);
            } else if (typeof(T) == typeof(TimeEntryModel)) {
                await Task.WhenAll (models.Select ((model) => DeleteTimeEntry (model as TimeEntryModel)));
            } else {
                throw new NotSupportedException ("Deleting of models (of type T) is not supported.");
            }
        }

        #region Client methods

        public Task CreateClient (ClientModel model)
        {
            var url = modelUrls [typeof(ClientModel)];

            throw new NotImplementedException ();
        }

        public Task<ClientModel> GetClient (long id)
        {
            var url = new Uri (modelUrls [typeof(ClientModel)], id.ToString ());

            throw new NotImplementedException ();
        }

        public Task<List<ClientModel>> ListClients ()
        {
            var url = modelUrls [typeof(ClientModel)];

            throw new NotImplementedException ();
        }

        public Task<List<ClientModel>> ListWorkspaceClients (long workspaceId)
        {
            throw new NotImplementedException ();
        }

        public Task UpdateClient (ClientModel model)
        {
            var url = modelUrls [typeof(ClientModel)];

            throw new NotImplementedException ();
        }

        public Task DeleteClient (ClientModel model)
        {
            var url = modelUrls [typeof(ClientModel)];

            throw new NotImplementedException ();
        }

        #endregion

        #region Project methods

        public Task CreateProject (ProjectModel model)
        {
            var url = modelUrls [typeof(ProjectModel)];

            throw new NotImplementedException ();
        }

        public Task<ProjectModel> GetProject (long id)
        {
            var url = new Uri (modelUrls [typeof(ProjectModel)], id.ToString ());

            throw new NotImplementedException ();
        }

        public Task<List<ProjectModel>> ListProjects ()
        {
            var url = modelUrls [typeof(ProjectModel)];

            throw new NotImplementedException ();
        }

        public Task<List<ProjectModel>> ListWorkspaceProjects (long workspaceId)
        {
            throw new NotImplementedException ();
        }

        public Task UpdateProject (ProjectModel model)
        {
            var url = modelUrls [typeof(ProjectModel)];

            throw new NotImplementedException ();
        }

        public Task DeleteProject (ProjectModel model)
        {
            var url = modelUrls [typeof(ProjectModel)];

            throw new NotImplementedException ();
        }

        public Task DeleteProjects (IEnumerable<ProjectModel> models)
        {
            var url = new Uri (modelUrls [typeof(ProjectModel)],
                          String.Join (",", models.Select ((model) => model.Id.ToString ())));

            throw new NotImplementedException ();
        }

        #endregion

        #region Task methods

        public Task CreateTask (TaskModel model)
        {
            var url = modelUrls [typeof(TaskModel)];

            throw new NotImplementedException ();
        }

        public Task<TaskModel> GetTask (long id)
        {
            var url = new Uri (modelUrls [typeof(TaskModel)], id.ToString ());

            throw new NotImplementedException ();
        }

        public Task<List<TaskModel>> ListTasks ()
        {
            var url = modelUrls [typeof(TaskModel)];

            throw new NotImplementedException ();
        }

        public Task<List<TaskModel>> ListProjectTasks (long projectId)
        {
            throw new NotImplementedException ();
        }

        public Task<List<TaskModel>> ListWorkspaceTasks (long workspaceId)
        {
            throw new NotImplementedException ();
        }

        public Task UpdateTask (TaskModel model)
        {
            var url = modelUrls [typeof(TaskModel)];

            throw new NotImplementedException ();
        }

        public Task DeleteTask (TaskModel model)
        {
            var url = modelUrls [typeof(TaskModel)];

            throw new NotImplementedException ();
        }

        public Task DeleteTasks (IEnumerable<TaskModel> models)
        {
            var url = new Uri (modelUrls [typeof(TaskModel)],
                          String.Join (",", models.Select ((model) => model.Id.ToString ())));

            throw new NotImplementedException ();
        }

        #endregion

        #region Time entry methods

        public Task CreateTimeEntry (TimeEntryModel model)
        {
            var url = modelUrls [typeof(TimeEntryModel)];

            throw new NotImplementedException ();
        }

        public Task<TimeEntryModel> GetTimeEntry (long id)
        {
            var url = new Uri (modelUrls [typeof(TimeEntryModel)], id.ToString ());

            throw new NotImplementedException ();
        }

        public Task<List<TimeEntryModel>> ListTimeEntries ()
        {
            var url = modelUrls [typeof(TimeEntryModel)];

            throw new NotImplementedException ();
        }

        public Task<List<TimeEntryModel>> ListTimeEntries (DateTime start, DateTime end)
        {
            var url = modelUrls [typeof(TimeEntryModel)];

            throw new NotImplementedException ();
        }

        public Task UpdateTimeEntry (TimeEntryModel model)
        {
            var url = modelUrls [typeof(TimeEntryModel)];

            throw new NotImplementedException ();
        }

        public Task DeleteTimeEntry (TimeEntryModel model)
        {
            var url = modelUrls [typeof(TimeEntryModel)];

            throw new NotImplementedException ();
        }

        #endregion

        #region Workspace methods

        public Task CreateWorkspace (WorkspaceModel model)
        {
            var url = modelUrls [typeof(WorkspaceModel)];

            throw new NotImplementedException ();
        }

        public Task<WorkspaceModel> GetWorkspace (long id)
        {
            var url = new Uri (modelUrls [typeof(WorkspaceModel)], id.ToString ());

            throw new NotImplementedException ();
        }

        public Task<List<WorkspaceModel>> ListWorkspaces ()
        {
            var url = modelUrls [typeof(WorkspaceModel)];

            throw new NotImplementedException ();
        }

        public Task UpdateWorkspace (WorkspaceModel model)
        {
            var url = modelUrls [typeof(WorkspaceModel)];

            throw new NotImplementedException ();
        }

        #endregion

        #region User methods

        public Task CreateUser (UserModel model)
        {
            var url = new Uri (v8Url, "signups");

            throw new NotImplementedException ();
        }

        public Task<UserModel> GetUser (long id)
        {
            // TODO: Check that the id is that of the currently logged in user
            var url = new Uri (v8Url, "me");

            throw new NotImplementedException ();
        }

        public Task UpdateUser (UserModel model)
        {
            // TODO: Check that the id is that of the currently logged in user
            var url = new Uri (v8Url, "me");

            throw new NotImplementedException ();
        }

        #endregion

    }
}

