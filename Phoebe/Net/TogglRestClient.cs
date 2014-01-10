using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Toggl.Phoebe.Data;

namespace Toggl.Phoebe.Net
{
    public class TogglRestClient : ITogglClient
    {
        private readonly Uri v8Url;
        private readonly Dictionary<Type, Uri> modelUrls;
        private readonly HttpClient httpClient;

        public TogglRestClient (Uri url)
        {
            v8Url = new Uri (url, "v8/");
            modelUrls = new Dictionary<Type, Uri> () {
                { typeof(ClientModel), new Uri (v8Url, "clients/") },
                { typeof(ProjectModel), new Uri (v8Url, "projects/") },
                { typeof(TaskModel), new Uri (v8Url, "tasks/") },
                { typeof(TimeEntryModel), new Uri (v8Url, "time_entries/") },
                { typeof(WorkspaceModel), new Uri (v8Url, "workspaces/") },
            };
            httpClient = new HttpClient ();
            var headers = httpClient.DefaultRequestHeaders;
            headers.UserAgent.Clear ();
            headers.UserAgent.Add (new ProductInfoHeaderValue ("TogglMobile", "0.01")); // TODO: Populate UserAgent
            headers.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));
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

        private string ModelToJson<T> (T model)
            where T : Model
        {
            string dataKey;
            if (typeof(T) == typeof(TimeEntryModel)) {
                dataKey = "time_entry";
            } else if (typeof(T) == typeof(ProjectModel)) {
                dataKey = "project";
            } else if (typeof(T) == typeof(ClientModel)) {
                dataKey = "client";
            } else if (typeof(T) == typeof(TaskModel)) {
                dataKey = "task";
            } else if (typeof(T) == typeof(WorkspaceModel)) {
                dataKey = "workspace";
            } else if (typeof(T) == typeof(UserModel)) {
                dataKey = "user";
            } else {
                throw new ArgumentException ("Don't know how to handle model of type T.", "model");
            }

            var json = new JObject ();
            json.Add (dataKey, JObject.FromObject (model));
            return json.ToString (Formatting.None);
        }

        private async Task CreateModel<T> (Uri url, T model)
            where T : Model, new()
        {
            var json = ModelToJson (model);
            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                Convert.ToBase64String (ASCIIEncoding.ASCII.GetBytes (
                    string.Format ("{0}:x", "apiToken")))); // TODO: Use real api token
            var httpResp = await httpClient.SendAsync (httpReq);

            var respData = await httpResp.Content.ReadAsStringAsync ();
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);
            model.Merge (wrap.Data);
            // In case the local model has changed in the mean time (and merge does nothing),
            // make sure that the remote id is set.
            if (model.RemoteId == null)
                model.RemoteId = wrap.Data.RemoteId;
        }

        #region Client methods

        public Task CreateClient (ClientModel model)
        {
            var url = modelUrls [typeof(ClientModel)];
            return CreateModel<ClientModel> (url, model);
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
            return CreateModel<ProjectModel> (url, model);
        }

        public Task<ProjectModel> GetProject (long id)
        {
            var url = new Uri (modelUrls [typeof(ProjectModel)], id.ToString ());

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
            return CreateModel<TaskModel> (url, model);
        }

        public Task<TaskModel> GetTask (long id)
        {
            var url = new Uri (modelUrls [typeof(TaskModel)], id.ToString ());

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
            return CreateModel<TimeEntryModel> (url, model);
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
            return CreateModel<WorkspaceModel> (url, model);
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

        public async Task<UserModel> GetUser (string username, string password)
        {
            var url = new Uri (v8Url, "me");

            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                Convert.ToBase64String (ASCIIEncoding.ASCII.GetBytes (
                    string.Format ("{0}:{1}", username, password))));
            var httpResp = await httpClient.SendAsync (httpReq);

            var respData = await httpResp.Content.ReadAsStringAsync ();
            var wrap = JsonConvert.DeserializeObject<Wrapper<UserModel>> (respData);

            return wrap.Data;
        }

        public Task UpdateUser (UserModel model)
        {
            // TODO: Check that the id is that of the currently logged in user
            var url = new Uri (v8Url, "me");

            throw new NotImplementedException ();
        }

        #endregion

        private class Wrapper<T>
        {
            [JsonProperty ("data")]
            public T Data { get; set; }

            [JsonProperty ("since", NullValueHandling = NullValueHandling.Ignore)]
            public long? Since { get; set; }
        }
    }
}

