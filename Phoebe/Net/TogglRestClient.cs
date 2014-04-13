using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class TogglRestClient : ITogglClient
    {
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private readonly Uri v8Url;
        private readonly HttpClient httpClient;

        public TogglRestClient (Uri url)
        {
            v8Url = new Uri (url, "v8/");
            httpClient = new HttpClient ();
            var headers = httpClient.DefaultRequestHeaders;
            headers.UserAgent.Clear ();
            headers.UserAgent.Add (new ProductInfoHeaderValue (Platform.AppIdentifier, Platform.AppVersion));
            headers.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));
        }

        public async Task Create<T> (T model)
            where T : Model
        {
            var type = model.GetType ();
            if (type == typeof(ClientModel)) {
                await CreateClient (model as ClientModel);
            } else if (type == typeof(ProjectModel)) {
                await CreateProject (model as ProjectModel);
            } else if (type == typeof(TaskModel)) {
                await CreateTask (model as TaskModel);
            } else if (type == typeof(TimeEntryModel)) {
                await CreateTimeEntry (model as TimeEntryModel);
            } else if (type == typeof(WorkspaceModel)) {
                await CreateWorkspace (model as WorkspaceModel);
            } else if (type == typeof(UserModel)) {
                await CreateUser (model as UserModel);
            } else if (type == typeof(TagModel)) {
                await CreateTag (model as TagModel);
            } else if (type == typeof(WorkspaceUserModel)) {
                await CreateWorkspaceUser (model as WorkspaceUserModel);
            } else if (type == typeof(ProjectUserModel)) {
                await CreateProjectUser (model as ProjectUserModel);
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
            var type = model.GetType ();
            if (type == typeof(ClientModel)) {
                await UpdateClient (model as ClientModel);
            } else if (type == typeof(ProjectModel)) {
                await UpdateProject (model as ProjectModel);
            } else if (type == typeof(TaskModel)) {
                await UpdateTask (model as TaskModel);
            } else if (type == typeof(TimeEntryModel)) {
                await UpdateTimeEntry (model as TimeEntryModel);
            } else if (type == typeof(WorkspaceModel)) {
                await UpdateWorkspace (model as WorkspaceModel);
            } else if (type == typeof(UserModel)) {
                await UpdateUser (model as UserModel);
            } else if (type == typeof(TagModel)) {
                await UpdateTag (model as TagModel);
            } else if (type == typeof(WorkspaceUserModel)) {
                await UpdateWorkspaceUser (model as WorkspaceUserModel);
            } else if (type == typeof(ProjectUserModel)) {
                await UpdateProjectUser (model as ProjectUserModel);
            } else {
                throw new NotSupportedException ("Updating of model (of type T) is not supported.");
            }
        }

        public async Task Delete<T> (T model)
            where T : Model
        {
            var type = model.GetType ();
            if (type == typeof(ClientModel)) {
                await DeleteClient (model as ClientModel);
            } else if (type == typeof(ProjectModel)) {
                await DeleteProject (model as ProjectModel);
            } else if (type == typeof(TaskModel)) {
                await DeleteTask (model as TaskModel);
            } else if (type == typeof(TimeEntryModel)) {
                await DeleteTimeEntry (model as TimeEntryModel);
            } else if (type == typeof(TagModel)) {
                await DeleteTag (model as TagModel);
            } else if (type == typeof(WorkspaceUserModel)) {
                await DeleteWorkspaceUser (model as WorkspaceUserModel);
            } else if (type == typeof(ProjectUserModel)) {
                await DeleteProjectUser (model as ProjectUserModel);
            } else {
                throw new NotSupportedException (String.Format ("Deleting of model (of type {0}) is not supported.", typeof(T)));
            }
        }

        public async Task Delete<T> (IEnumerable<T> models)
            where T : Model
        {
            if (typeof(T) == typeof(ClientModel)) {
                await Task.WhenAll (models.Select ((object model) => DeleteClient ((ClientModel)model)));
            } else if (typeof(T) == typeof(ProjectModel)) {
                await DeleteProjects (models as IEnumerable<ProjectModel>);
            } else if (typeof(T) == typeof(TaskModel)) {
                await DeleteTasks (models as IEnumerable<TaskModel>);
            } else if (typeof(T) == typeof(TimeEntryModel)) {
                await Task.WhenAll (models.Select ((object model) => DeleteTimeEntry ((TimeEntryModel)model)));
            } else if (typeof(T) == typeof(Model)) {
                // Cannot use LINQ due to AOT failure when using lambdas that use generic method calls inside them.
                var tasks = new List<Task> ();
                foreach (var model in models) {
                    tasks.Add (Delete (model));
                }
                await Task.WhenAll (tasks);
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
            } else if (typeof(T) == typeof(TagModel)) {
                dataKey = "tag";
            } else if (typeof(T) == typeof(WorkspaceUserModel)) {
                dataKey = "workspace_user";
            } else if (typeof(T) == typeof(ProjectUserModel)) {
                dataKey = "project_user";
            } else {
                throw new ArgumentException ("Don't know how to handle model of type T.", "model");
            }

            var json = new JObject ();
            json.Add (dataKey, JObject.FromObject (model));
            return json.ToString (Formatting.None);
        }

        private HttpRequestMessage SetupRequest (HttpRequestMessage req)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.Token != null) {
                req.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                    Convert.ToBase64String (ASCIIEncoding.ASCII.GetBytes (
                        string.Format ("{0}:api_token", authManager.Token))));
            }
            return req;
        }

        private void PrepareResponse (HttpResponseMessage resp)
        {
            ServiceContainer.Resolve<MessageBus> ().Send (new TogglHttpResponseMessage (this, resp));
            resp.EnsureSuccessStatusCode ();
        }

        private async Task CreateModel<T> (Uri url, T model, Action<T, T> merger = null)
            where T : Model, new()
        {
            var json = ModelToJson (model);
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            });
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);
            if (merger != null)
                merger (model, wrap.Data);
            else
                model.Merge (wrap.Data);
            // In case the local model has changed in the mean time (and merge does nothing),
            // make sure that the remote id is set.
            if (model.RemoteId == null)
                model.RemoteId = wrap.Data.RemoteId;
        }

        private async Task<T> GetModel<T> (Uri url, Func<T, T> selector = null)
            where T : Model, new()
        {
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            });
            selector = selector ?? Model.Update;

            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);

            return selector (wrap.Data);
        }

        private async Task UpdateModel<T> (Uri url, T model, Func<T, T> selector = null)
            where T : Model, new()
        {
            var json = ModelToJson (model);
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Put,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            });
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);
            if (selector != null)
                wrap.Data = selector (wrap.Data);
            model.Merge (wrap.Data);
        }

        private async Task<List<T>> ListModels<T> (Uri url, Func<T, T> selector = null)
            where T : Model, new()
        {
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            });

            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var models = JsonConvert.DeserializeObject<List<T>> (respData) ?? Enumerable.Empty<T> ();

            return models.Select (selector ?? Model.Update).ToList ();
        }

        private async Task DeleteModel (Uri url)
        {
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Delete,
                RequestUri = url,
            });
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);
        }

        private Task DeleteModels (Uri url)
        {
            return DeleteModel (url);
        }

        #region Client methods

        public Task CreateClient (ClientModel model)
        {
            var url = new Uri (v8Url, "clients");
            return CreateModel (url, model);
        }

        public Task<ClientModel> GetClient (long id)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", id.ToString ()));
            return GetModel<ClientModel> (url);
        }

        public Task<List<ClientModel>> ListClients ()
        {
            var url = new Uri (v8Url, "clients");
            return ListModels<ClientModel> (url);
        }

        public Task<List<ClientModel>> ListWorkspaceClients (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/clients", workspaceId.ToString ()));
            return ListModels<ClientModel> (url);
        }

        public Task UpdateClient (ClientModel model)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", model.RemoteId.Value.ToString ()));
            return UpdateModel (url, model);
        }

        public Task DeleteClient (ClientModel model)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", model.RemoteId.Value.ToString ()));
            return DeleteModel (url);
        }

        #endregion

        #region Project methods

        public Task CreateProject (ProjectModel model)
        {
            var url = new Uri (v8Url, "projects");
            return CreateModel (url, model);
        }

        public Task<ProjectModel> GetProject (long id)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", id.ToString ()));
            return GetModel<ProjectModel> (url);
        }

        public Task<List<ProjectModel>> ListWorkspaceProjects (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/projects", workspaceId.ToString ()));
            return ListModels<ProjectModel> (url);
        }

        public Task<List<WorkspaceUserModel>> ListWorkspaceUsers (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/workspace_users", workspaceId.ToString ()));
            return ListModels<WorkspaceUserModel> (url);
        }

        public Task UpdateProject (ProjectModel model)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", model.RemoteId.Value.ToString ()));
            return UpdateModel (url, model);
        }

        public Task DeleteProject (ProjectModel model)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", model.RemoteId.Value.ToString ()));
            return DeleteModel (url);
        }

        public Task DeleteProjects (IEnumerable<ProjectModel> models)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}",
                          String.Join (",", models.Select ((model) => model.Id.Value.ToString ()))));
            return DeleteModels (url);
        }

        #endregion

        #region Task methods

        public Task CreateTask (TaskModel model)
        {
            var url = new Uri (v8Url, "tasks");
            return CreateModel (url, model);
        }

        public Task<TaskModel> GetTask (long id)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", id.ToString ()));
            return GetModel<TaskModel> (url);
        }

        public Task<List<TaskModel>> ListProjectTasks (long projectId)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}/tasks", projectId.ToString ()));
            return ListModels<TaskModel> (url);
        }

        public Task<List<ProjectUserModel>> ListProjectUsers (long projectId)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}/project_users", projectId.ToString ()));
            return ListModels<ProjectUserModel> (url);
        }

        public Task<List<TaskModel>> ListWorkspaceTasks (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/tasks", workspaceId.ToString ()));
            return ListModels<TaskModel> (url);
        }

        public Task UpdateTask (TaskModel model)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", model.RemoteId.Value.ToString ()));
            return UpdateModel (url, model);
        }

        public Task DeleteTask (TaskModel model)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", model.RemoteId.Value.ToString ()));
            return DeleteModel (url);
        }

        public Task DeleteTasks (IEnumerable<TaskModel> models)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}",
                          String.Join (",", models.Select ((model) => model.Id.Value.ToString ()))));
            return DeleteModels (url);
        }

        #endregion

        #region Time entry methods

        public Task CreateTimeEntry (TimeEntryModel model)
        {
            var url = new Uri (v8Url, "time_entries");
            model.CreatedWith = Platform.DefaultCreatedWith;
            return CreateModel (url, model, (te1, te2) => {
                // Assign the correct user to the response from the server
                te2.User = ServiceContainer.Resolve<AuthManager> ().User;
                te1.Merge (te2);
            });
        }

        public Task<TimeEntryModel> GetTimeEntry (long id)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", id.ToString ()));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return GetModel<TimeEntryModel> (url, (te) => {
                te.User = user;
                return Model.Update (te);
            });
        }

        public Task<List<TimeEntryModel>> ListTimeEntries ()
        {
            var url = new Uri (v8Url, "time_entries");
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return ListModels<TimeEntryModel> (url, (te) => {
                te.User = user;
                return Model.Update (te);
            });
        }

        public Task<List<TimeEntryModel>> ListTimeEntries (DateTime start, DateTime end)
        {
            var url = new Uri (v8Url,
                          String.Format ("time_entries?start_date={0}&end_date={1}",
                              WebUtility.UrlEncode (start.ToUtc ().ToString ("o")),
                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o"))));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return ListModels<TimeEntryModel> (url, (te) => {
                te.User = user;
                return Model.Update (te);
            });
        }

        public Task<List<TimeEntryModel>> ListTimeEntries (DateTime end, int days)
        {
            var url = new Uri (v8Url,
                          String.Format ("time_entries?end_date={0}&num_of_days={1}",
                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o")),
                              days));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return ListModels<TimeEntryModel> (url, (te) => {
                te.User = user;
                return Model.Update (te);
            });
        }

        public Task UpdateTimeEntry (TimeEntryModel model)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", model.RemoteId.Value.ToString ()));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return UpdateModel (url, model, (te) => {
                te.User = user;
                return te;
            });
        }

        public Task DeleteTimeEntry (TimeEntryModel model)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", model.RemoteId.Value.ToString ()));
            return DeleteModel (url);
        }

        #endregion

        #region Workspace methods

        public Task CreateWorkspace (WorkspaceModel model)
        {
            var url = new Uri (v8Url, "workspaces");
            return CreateModel (url, model);
        }

        public Task<WorkspaceModel> GetWorkspace (long id)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}", id.ToString ()));
            return GetModel<WorkspaceModel> (url);
        }

        public Task<List<WorkspaceModel>> ListWorkspaces ()
        {
            var url = new Uri (v8Url, "workspaces");
            return ListModels<WorkspaceModel> (url);
        }

        public Task UpdateWorkspace (WorkspaceModel model)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}", model.RemoteId.Value.ToString ()));
            return UpdateModel (url, model);
        }

        #endregion

        #region Tag methods

        public Task CreateTag (TagModel model)
        {
            var url = new Uri (v8Url, "tags");
            return CreateModel (url, model);
        }

        public Task UpdateTag (TagModel model)
        {
            var url = new Uri (v8Url, String.Format ("tags/{0}", model.RemoteId.Value.ToString ()));
            return UpdateModel (url, model);
        }

        public Task DeleteTag (TagModel model)
        {
            var url = new Uri (v8Url, String.Format ("tags/{0}", model.RemoteId.Value.ToString ()));
            return DeleteModel (url);
        }

        #endregion

        #region Workspace user methods

        public async Task CreateWorkspaceUser (WorkspaceUserModel model)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/invite", model.From.RemoteId.Value.ToString ()));

            var json = JsonConvert.SerializeObject (new {
                emails = new string[] { model.To.Email },
            });
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            });
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var wrap = JObject.Parse (await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false));
            var data = wrap ["data"] [0].ToObject<WorkspaceUserModel> ();
            model.Merge (data);
            // In case the local model has changed in the mean time (and merge does nothing),
            // make sure that the remote id is set.
            if (model.RemoteId == null)
                model.RemoteId = data.RemoteId;
        }

        public Task UpdateWorkspaceUser (WorkspaceUserModel model)
        {
            var url = new Uri (v8Url, String.Format ("workspace_users/{0}", model.RemoteId.Value.ToString ()));
            return UpdateModel (url, model);
        }

        public Task DeleteWorkspaceUser (WorkspaceUserModel model)
        {
            var url = new Uri (v8Url, String.Format ("workspace_users/{0}", model.RemoteId.Value.ToString ()));
            return DeleteModel (url);
        }

        #endregion

        #region Project user methods

        public Task CreateProjectUser (ProjectUserModel model)
        {
            var url = new Uri (v8Url, "project_users");
            return CreateModel (url, model);
        }

        public Task UpdateProjectUser (ProjectUserModel model)
        {
            var url = new Uri (v8Url, String.Format ("project_users/{0}", model.RemoteId.Value.ToString ()));
            return UpdateModel (url, model);
        }

        public Task DeleteProjectUser (ProjectUserModel model)
        {
            var url = new Uri (v8Url, String.Format ("project_users/{0}", model.RemoteId.Value.ToString ()));
            return DeleteModel (url);
        }

        #endregion

        #region User methods

        public Task CreateUser (UserModel model)
        {
            var url = new Uri (v8Url, model.GoogleAccessToken != null ? "signups?app_name=toggl_mobile" : "signups");
            model.CreatedWith = Platform.DefaultCreatedWith;
            return CreateModel (url, model);
        }

        public Task<UserModel> GetUser (long id)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.Token == null || authManager.User.RemoteId != (long?)id)
                throw new NotSupportedException ("Can only update currently logged in user.");

            var url = new Uri (v8Url, "me");
            return GetModel<UserModel> (url);
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
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<UserModel>> (respData);

            return Model.Update (wrap.Data);
        }

        public async Task<UserModel> GetUser (string googleAccessToken)
        {
            var url = new Uri (v8Url, "me?app_name=toggl_mobile");

            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                Convert.ToBase64String (ASCIIEncoding.ASCII.GetBytes (
                    string.Format ("{0}:{1}", googleAccessToken, "google_access_token"))));
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<UserModel>> (respData);

            return Model.Update (wrap.Data);
        }

        public Task UpdateUser (UserModel model)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.Token == null || authManager.UserId != model.Id)
                throw new NotSupportedException ("Can only update currently logged in user.");

            var url = new Uri (v8Url, "me");
            return UpdateModel (url, model);
        }

        #endregion

        public async Task<UserRelatedModels> GetChanges (DateTime? since)
        {
            since = since.ToUtc ();
            var relUrl = "me?with_related_data=true";
            if (since.HasValue)
                relUrl = String.Format ("{0}&since={1}", relUrl, (long)(since.Value - UnixStart).TotalSeconds);
            var url = new Uri (v8Url, relUrl);

            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            });
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var json = JObject.Parse (respData);

            var user = Model.Update (json ["data"].ToObject<UserModel> ());
            return new UserRelatedModels () {
                Timestamp = UnixStart + TimeSpan.FromSeconds ((long)json ["since"]),
                User = user,
                Workspaces = GetChangesModels<WorkspaceModel> (json ["data"] ["workspaces"]),
                Tags = GetChangesTagModels (json ["data"] ["tags"]),
                Clients = GetChangesModels<ClientModel> (json ["data"] ["clients"]),
                Projects = GetChangesModels<ProjectModel> (json ["data"] ["projects"]),
                Tasks = GetChangesModels<TaskModel> (json ["data"] ["tasks"]),
                TimeEntries = GetChangesTimeEntryModels (json ["data"] ["time_entries"], user),
            };
        }

        private IEnumerable<T> GetChangesModels<T> (JToken json)
            where T : Model, new()
        {
            if (json == null)
                return Enumerable.Empty<T> ();
            return json.ToObject<List<T>> ().Select (Model.Update);
        }

        private IEnumerable<TagModel> GetChangesTagModels (JToken json)
        {
            if (json == null)
                return Enumerable.Empty<TagModel> ();
            return json.ToObject<List<TagModel>> ().Select (MergeTag);
        }

        private TagModel MergeTag (TagModel model)
        {
            // Try find existing tag with which to merge:
            IEnumerable<TagModel> existingTags;
            // Load database entries into memory
            existingTags = Model.Query<TagModel> ((m) =>
                (m.Name == model.Name && m.RemoteId == null) || m.RemoteId == model.RemoteId).ToList ();
            // Look through the cache to discover the correct items
            existingTags = Model.Manager.Cached<TagModel> ().Where ((m) =>
                (m.Name == model.Name && m.RemoteId == null) || m.RemoteId == model.RemoteId).ToList ();

            var tag = existingTags.Where ((m) => m.RemoteId == model.RemoteId).FirstOrDefault ();
            if (tag == null)
                tag = existingTags.FirstOrDefault ();

            if (tag != null) {
                // Merge with existing:
                tag.Merge (model);
                // Make sure the remote ID is set for this tag, even if the merge failed
                if (tag.RemoteId == null) {
                    tag.RemoteId = model.RemoteId;
                }
            } else {
                // Nothing to merge with, insert
                tag = Model.Update (model);
            }

            return tag;
        }

        private IEnumerable<TimeEntryModel> GetChangesTimeEntryModels (JToken json, UserModel user)
        {
            if (json == null)
                return Enumerable.Empty<TimeEntryModel> ();
            return json.ToObject<List<TimeEntryModel>> ().Select ((te) => {
                te.User = user;
                return Model.Update (te);
            });
        }

        private class Wrapper<T>
        {
            [JsonProperty ("data")]
            public T Data { get; set; }

            [JsonProperty ("since", NullValueHandling = NullValueHandling.Ignore)]
            public long? Since { get; set; }
        }
    }
}

