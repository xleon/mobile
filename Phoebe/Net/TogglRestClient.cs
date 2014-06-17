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
using Toggl.Phoebe.Data.Json;
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

        public async Task<T> Create<T> (T jsonObject)
            where T : CommonJson
        {
            var type = jsonObject.GetType ();
            if (type == typeof(ClientJson)) {
                return (T)(object)await CreateClient ((ClientJson)(object)jsonObject);
            } else if (type == typeof(ProjectJson)) {
                return (T)(object)await CreateProject ((ProjectJson)(object)jsonObject);
            } else if (type == typeof(TaskJson)) {
                return (T)(object)await CreateTask ((TaskJson)(object)jsonObject);
            } else if (type == typeof(TimeEntryJson)) {
                return (T)(object)await CreateTimeEntry ((TimeEntryJson)(object)jsonObject);
            } else if (type == typeof(WorkspaceJson)) {
                return (T)(object)await CreateWorkspace ((WorkspaceJson)(object)jsonObject);
            } else if (type == typeof(UserJson)) {
                return (T)(object)await CreateUser ((UserJson)(object)jsonObject);
            } else if (type == typeof(TagJson)) {
                return (T)(object)await CreateTag ((TagJson)(object)jsonObject);
            } else if (type == typeof(WorkspaceUserJson)) {
                return (T)(object)await CreateWorkspaceUser ((WorkspaceUserJson)(object)jsonObject);
            } else if (type == typeof(ProjectUserJson)) {
                return (T)(object)await CreateProjectUser ((ProjectUserJson)(object)jsonObject);
            } else {
                throw new NotSupportedException (String.Format ("Creating of {0} is not supported.", type));
            }
        }

        public async Task<T> Get<T> (long id)
            where T : CommonJson
        {
            var type = typeof(T);
            if (type == typeof(ClientJson)) {
                return (T)(object)await GetClient (id);
            } else if (type == typeof(ProjectJson)) {
                return (T)(object)await GetProject (id);
            } else if (type == typeof(TaskJson)) {
                return (T)(object)await GetTask (id);
            } else if (type == typeof(TimeEntryJson)) {
                return (T)(object)await GetTimeEntry (id);
            } else if (type == typeof(WorkspaceJson)) {
                return (T)(object)await GetWorkspace (id);
            } else if (type == typeof(UserJson)) {
                return (T)(object)await GetUser (id);
            } else {
                throw new NotSupportedException (String.Format ("Fetching of {0} is not supported.", type));
            }
        }

        public async Task<List<T>> List<T> ()
            where T : CommonJson
        {
            var type = typeof(T);
            if (type == typeof(ClientJson)) {
                return (List<T>)(object)await ListClients ();
            } else if (type == typeof(TimeEntryJson)) {
                return (List<T>)(object)await ListTimeEntries ();
            } else if (type == typeof(WorkspaceJson)) {
                return (List<T>)(object)await ListWorkspaces ();
            } else {
                throw new NotSupportedException (String.Format ("Listing of {0} is not supported.", type));
            }
        }

        public async Task<T> Update<T> (T jsonObject)
            where T : CommonJson
        {
            var type = jsonObject.GetType ();
            if (type == typeof(ClientJson)) {
                return (T)(object)await UpdateClient ((ClientJson)(object)jsonObject);
            } else if (type == typeof(ProjectJson)) {
                return (T)(object)await UpdateProject ((ProjectJson)(object)jsonObject);
            } else if (type == typeof(TaskJson)) {
                return (T)(object)await UpdateTask ((TaskJson)(object)jsonObject);
            } else if (type == typeof(TimeEntryJson)) {
                return (T)(object)await UpdateTimeEntry ((TimeEntryJson)(object)jsonObject);
            } else if (type == typeof(WorkspaceJson)) {
                return (T)(object)await UpdateWorkspace ((WorkspaceJson)(object)jsonObject);
            } else if (type == typeof(UserJson)) {
                return (T)(object)await UpdateUser ((UserJson)(object)jsonObject);
            } else if (type == typeof(TagJson)) {
                return (T)(object)await UpdateTag ((TagJson)(object)jsonObject);
            } else if (type == typeof(WorkspaceUserJson)) {
                return (T)(object)await UpdateWorkspaceUser ((WorkspaceUserJson)(object)jsonObject);
            } else if (type == typeof(ProjectUserJson)) {
                return (T)(object)await UpdateProjectUser ((ProjectUserJson)(object)jsonObject);
            } else {
                throw new NotSupportedException (String.Format ("Updating of {0} is not supported.", type));
            }
        }

        public async Task Delete<T> (T jsonObject)
            where T : CommonJson
        {
            var type = jsonObject.GetType ();
            if (type == typeof(ClientJson)) {
                await DeleteClient ((ClientJson)(object)jsonObject);
            } else if (type == typeof(ProjectJson)) {
                await DeleteProject ((ProjectJson)(object)jsonObject);
            } else if (type == typeof(TaskJson)) {
                await DeleteTask ((TaskJson)(object)jsonObject);
            } else if (type == typeof(TimeEntryJson)) {
                await DeleteTimeEntry ((TimeEntryJson)(object)jsonObject);
            } else if (type == typeof(TagJson)) {
                await DeleteTag ((TagJson)(object)jsonObject);
            } else if (type == typeof(WorkspaceUserJson)) {
                await DeleteWorkspaceUser ((WorkspaceUserJson)(object)jsonObject);
            } else if (type == typeof(ProjectUserJson)) {
                await DeleteProjectUser ((ProjectUserJson)(object)jsonObject);
            } else {
                throw new NotSupportedException (String.Format ("Deleting of {0} is not supported.", type));
            }
        }

        public async Task Delete<T> (IEnumerable<T> jsonObjects)
            where T : CommonJson
        {
            var type = typeof(T);
            if (type == typeof(ClientJson)) {
                await Task.WhenAll (jsonObjects.Select ((object json) => DeleteClient ((ClientJson)json)));
            } else if (type == typeof(ProjectJson)) {
                await DeleteProjects (jsonObjects as IEnumerable<ProjectJson>);
            } else if (type == typeof(TaskJson)) {
                await DeleteTasks (jsonObjects as IEnumerable<TaskJson>);
            } else if (type == typeof(TimeEntryJson)) {
                await Task.WhenAll (jsonObjects.Select ((object json) => DeleteTimeEntry ((TimeEntryJson)json)));
            } else if (type == typeof(CommonJson)) {
                // Cannot use LINQ due to AOT failure when using lambdas that use generic method calls inside them.
                var tasks = new List<Task> ();
                foreach (var json in jsonObjects) {
                    tasks.Add (Delete (json));
                }
                await Task.WhenAll (tasks);
            } else {
                throw new NotSupportedException (String.Format ("Batch deleting of {0} is not supported.", type));
            }
        }

        private string StringifyJson (CommonJson jsonObject)
        {
            var type = jsonObject.GetType ();

            string dataKey;
            if (type == typeof(TimeEntryJson)) {
                dataKey = "time_entry";
            } else if (type == typeof(ProjectJson)) {
                dataKey = "project";
            } else if (type == typeof(ClientJson)) {
                dataKey = "client";
            } else if (type == typeof(TaskJson)) {
                dataKey = "task";
            } else if (type == typeof(WorkspaceJson)) {
                dataKey = "workspace";
            } else if (type == typeof(UserJson)) {
                dataKey = "user";
            } else if (type == typeof(TagJson)) {
                dataKey = "tag";
            } else if (type == typeof(WorkspaceUserJson)) {
                dataKey = "workspace_user";
            } else if (type == typeof(ProjectUserJson)) {
                dataKey = "project_user";
            } else {
                throw new ArgumentException (String.Format ("Don't know how to handle JSON object of type {0}.", type), "jsonObject");
            }

            var json = new JObject ();
            json.Add (dataKey, JObject.FromObject (jsonObject));
            return json.ToString (Formatting.None);
        }

        private HttpRequestMessage SetupRequest (HttpRequestMessage req)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.Token != null) {
                req.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                    Convert.ToBase64String (Encoding.ASCII.GetBytes (
                        string.Format ("{0}:api_token", authManager.Token))));
            }
            return req;
        }

        private void PrepareResponse (HttpResponseMessage resp)
        {
            ServiceContainer.Resolve<MessageBus> ().Send (new TogglHttpResponseMessage (this, resp));
            if (resp.StatusCode == HttpStatusCode.BadRequest) {
                throw new ServerValidationException ();
            } else if (!resp.IsSuccessStatusCode) {
                throw new HttpRequestException (String.Format ("{0} ({1})", (int)resp.StatusCode, resp.ReasonPhrase));
            }
        }

        private async Task<T> CreateObject<T> (Uri url, T jsonObject)
            where T : CommonJson, new()
        {
            var json = StringifyJson (jsonObject);
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
            return wrap.Data;
        }

        private async Task<T> GetObject<T> (Uri url)
            where T : CommonJson, new()
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
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);
            return wrap.Data;
        }

        private async Task<T> UpdateObject<T> (Uri url, T jsonObject)
            where T : CommonJson, new()
        {
            var json = StringifyJson (jsonObject);
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
            return wrap.Data;
        }

        private async Task<List<T>> ListObjects<T> (Uri url)
            where T : CommonJson, new()
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
            return JsonConvert.DeserializeObject<List<T>> (respData) ?? new List<T> (0);
        }

        private async Task DeleteObject (Uri url)
        {
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Delete,
                RequestUri = url,
            });
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);
        }

        private Task DeleteObjects (Uri url)
        {
            return DeleteObject (url);
        }

        #region Client methods

        public Task<ClientJson> CreateClient (ClientJson jsonObject)
        {
            var url = new Uri (v8Url, "clients");
            return CreateObject (url, jsonObject);
        }

        public Task<ClientJson> GetClient (long id)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", id.ToString ()));
            return GetObject<ClientJson> (url);
        }

        public Task<List<ClientJson>> ListClients ()
        {
            var url = new Uri (v8Url, "clients");
            return ListObjects<ClientJson> (url);
        }

        public Task<List<ClientJson>> ListWorkspaceClients (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/clients", workspaceId.ToString ()));
            return ListObjects<ClientJson> (url);
        }

        public Task<ClientJson> UpdateClient (ClientJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", jsonObject.Id.Value.ToString ()));
            return UpdateObject (url, jsonObject);
        }

        public Task DeleteClient (ClientJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", jsonObject.Id.Value.ToString ()));
            return DeleteObject (url);
        }

        #endregion

        #region Project methods

        public Task<ProjectJson> CreateProject (ProjectJson jsonObject)
        {
            var url = new Uri (v8Url, "projects");
            return CreateObject (url, jsonObject);
        }

        public Task<ProjectJson> GetProject (long id)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", id.ToString ()));
            return GetObject<ProjectJson> (url);
        }

        public Task<List<ProjectJson>> ListWorkspaceProjects (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/projects", workspaceId.ToString ()));
            return ListObjects<ProjectJson> (url);
        }

        public Task<List<WorkspaceUserJson>> ListWorkspaceUsers (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/workspace_users", workspaceId.ToString ()));
            return ListObjects<WorkspaceUserJson> (url);
        }

        public Task<ProjectJson> UpdateProject (ProjectJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", jsonObject.Id.Value.ToString ()));
            return UpdateObject (url, jsonObject);
        }

        public Task DeleteProject (ProjectJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", jsonObject.Id.Value.ToString ()));
            return DeleteObject (url);
        }

        public Task DeleteProjects (IEnumerable<ProjectJson> jsonObjects)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}",
                          String.Join (",", jsonObjects.Select ((model) => model.Id.Value.ToString ()))));
            return DeleteObjects (url);
        }

        #endregion

        #region Task methods

        public Task<TaskJson> CreateTask (TaskJson jsonObject)
        {
            var url = new Uri (v8Url, "tasks");
            return CreateObject (url, jsonObject);
        }

        public Task<TaskJson> GetTask (long id)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", id.ToString ()));
            return GetObject<TaskJson> (url);
        }

        public Task<List<TaskJson>> ListProjectTasks (long projectId)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}/tasks", projectId.ToString ()));
            return ListObjects<TaskJson> (url);
        }

        public Task<List<ProjectUserJson>> ListProjectUsers (long projectId)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}/project_users", projectId.ToString ()));
            return ListObjects<ProjectUserJson> (url);
        }

        public Task<List<TaskJson>> ListWorkspaceTasks (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/tasks", workspaceId.ToString ()));
            return ListObjects<TaskJson> (url);
        }

        public Task<TaskJson> UpdateTask (TaskJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", jsonObject.Id.Value.ToString ()));
            return UpdateObject (url, jsonObject);
        }

        public Task DeleteTask (TaskJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", jsonObject.Id.Value.ToString ()));
            return DeleteObject (url);
        }

        public Task DeleteTasks (IEnumerable<TaskJson> jsonObjects)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}",
                          String.Join (",", jsonObjects.Select ((json) => json.Id.Value.ToString ()))));
            return DeleteObjects (url);
        }

        #endregion

        #region Time entry methods

        public Task<TimeEntryJson> CreateTimeEntry (TimeEntryJson jsonObject)
        {
            var url = new Uri (v8Url, "time_entries");
            jsonObject.CreatedWith = Platform.DefaultCreatedWith;
            return CreateObject (url, jsonObject);
        }

        public Task<TimeEntryJson> GetTimeEntry (long id)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", id.ToString ()));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return GetObject<TimeEntryJson> (url);
        }

        public Task<List<TimeEntryJson>> ListTimeEntries ()
        {
            var url = new Uri (v8Url, "time_entries");
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return ListObjects<TimeEntryJson> (url);
        }

        public Task<List<TimeEntryJson>> ListTimeEntries (DateTime start, DateTime end)
        {
            var url = new Uri (v8Url,
                          String.Format ("time_entries?start_date={0}&end_date={1}",
                              WebUtility.UrlEncode (start.ToUtc ().ToString ("o")),
                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o"))));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return ListObjects<TimeEntryJson> (url);
        }

        public Task<List<TimeEntryJson>> ListTimeEntries (DateTime end, int days)
        {
            var url = new Uri (v8Url,
                          String.Format ("time_entries?end_date={0}&num_of_days={1}",
                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o")),
                              days));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return ListObjects<TimeEntryJson> (url);
        }

        public Task<TimeEntryJson> UpdateTimeEntry (TimeEntryJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", jsonObject.Id.Value.ToString ()));
            var user = ServiceContainer.Resolve<AuthManager> ().User;
            return UpdateObject (url, jsonObject);
        }

        public Task DeleteTimeEntry (TimeEntryJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", jsonObject.Id.Value.ToString ()));
            return DeleteObject (url);
        }

        #endregion

        #region Workspace methods

        public Task<WorkspaceJson> CreateWorkspace (WorkspaceJson jsonObject)
        {
            var url = new Uri (v8Url, "workspaces");
            return CreateObject (url, jsonObject);
        }

        public Task<WorkspaceJson> GetWorkspace (long id)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}", id.ToString ()));
            return GetObject<WorkspaceJson> (url);
        }

        public Task<List<WorkspaceJson>> ListWorkspaces ()
        {
            var url = new Uri (v8Url, "workspaces");
            return ListObjects<WorkspaceJson> (url);
        }

        public Task<WorkspaceJson> UpdateWorkspace (WorkspaceJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}", jsonObject.Id.Value.ToString ()));
            return UpdateObject (url, jsonObject);
        }

        #endregion

        #region Tag methods

        public Task<TagJson> CreateTag (TagJson jsonObject)
        {
            var url = new Uri (v8Url, "tags");
            return CreateObject (url, jsonObject);
        }

        public Task<TagJson> UpdateTag (TagJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("tags/{0}", jsonObject.Id.Value.ToString ()));
            return UpdateObject (url, jsonObject);
        }

        public Task DeleteTag (TagJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("tags/{0}", jsonObject.Id.Value.ToString ()));
            return DeleteObject (url);
        }

        #endregion

        #region Workspace user methods

        public async Task<WorkspaceUserJson> CreateWorkspaceUser (WorkspaceUserJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/invite", jsonObject.WorkspaceId.ToString ()));

            var json = JsonConvert.SerializeObject (new {
                emails = new string[] { jsonObject.Email },
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
            var data = wrap ["data"] [0].ToObject<WorkspaceUserJson> ();
            return data;
        }

        public Task<WorkspaceUserJson> UpdateWorkspaceUser (WorkspaceUserJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("workspace_users/{0}", jsonObject.Id.Value.ToString ()));
            return UpdateObject (url, jsonObject);
        }

        public Task DeleteWorkspaceUser (WorkspaceUserJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("workspace_users/{0}", jsonObject.Id.Value.ToString ()));
            return DeleteObject (url);
        }

        #endregion

        #region Project user methods

        public Task<ProjectUserJson> CreateProjectUser (ProjectUserJson jsonObject)
        {
            var url = new Uri (v8Url, "project_users");
            return CreateObject (url, jsonObject);
        }

        public Task<ProjectUserJson> UpdateProjectUser (ProjectUserJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("project_users/{0}", jsonObject.Id.Value.ToString ()));
            return UpdateObject (url, jsonObject);
        }

        public Task DeleteProjectUser (ProjectUserJson jsonObject)
        {
            var url = new Uri (v8Url, String.Format ("project_users/{0}", jsonObject.Id.Value.ToString ()));
            return DeleteObject (url);
        }

        #endregion

        #region User methods

        public Task<UserJson> CreateUser (UserJson jsonObject)
        {
            var url = new Uri (v8Url, jsonObject.GoogleAccessToken != null ? "signups?app_name=toggl_mobile" : "signups");
            jsonObject.CreatedWith = Platform.DefaultCreatedWith;
            return CreateObject (url, jsonObject);
        }

        public Task<UserJson> GetUser (long id)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.Token == null || authManager.User.RemoteId != (long?)id)
                throw new NotSupportedException ("Can only update currently logged in user.");

            var url = new Uri (v8Url, "me");
            return GetObject<UserJson> (url);
        }

        public async Task<UserJson> GetUser (string username, string password)
        {
            var url = new Uri (v8Url, "me");

            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                Convert.ToBase64String (Encoding.ASCII.GetBytes (
                    string.Format ("{0}:{1}", username, password))));
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<UserJson>> (respData);

            return wrap.Data;
        }

        public async Task<UserJson> GetUser (string googleAccessToken)
        {
            var url = new Uri (v8Url, "me?app_name=toggl_mobile");

            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                Convert.ToBase64String (Encoding.ASCII.GetBytes (
                    string.Format ("{0}:{1}", googleAccessToken, "google_access_token"))));
            var httpResp = await httpClient.SendAsync (httpReq)
                .ConfigureAwait (continueOnCapturedContext: false);
            PrepareResponse (httpResp);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                .ConfigureAwait (continueOnCapturedContext: false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<UserJson>> (respData);

            return wrap.Data;
        }

        public Task<UserJson> UpdateUser (UserJson jsonObject)
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.Token == null || authManager.UserId != jsonObject.Id)
                throw new NotSupportedException ("Can only update currently logged in user.");

            var url = new Uri (v8Url, "me");
            return UpdateObject (url, jsonObject);
        }

        #endregion

        public async Task<UserRelatedJson> GetChanges (DateTime? since)
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

            var user = json ["data"].ToObject<UserJson> ();
            return new UserRelatedJson () {
                Timestamp = UnixStart + TimeSpan.FromSeconds ((long)json ["since"]),
                User = user,
                Workspaces = GetChangesObjects<WorkspaceJson> (json ["data"] ["workspaces"]),
                Tags = GetChangesObjects<TagJson> (json ["data"] ["tags"]),
                Clients = GetChangesObjects<ClientJson> (json ["data"] ["clients"]),
                Projects = GetChangesObjects<ProjectJson> (json ["data"] ["projects"]),
                Tasks = GetChangesObjects<TaskJson> (json ["data"] ["tasks"]),
                TimeEntries = GetChangesTimeEntryObjects (json ["data"] ["time_entries"], user),
            };
        }

        private IEnumerable<T> GetChangesObjects<T> (JToken json)
            where T : CommonJson, new()
        {
            if (json == null)
                return Enumerable.Empty<T> ();
            return json.ToObject<List<T>> ();
        }

        private IEnumerable<TimeEntryJson> GetChangesTimeEntryObjects (JToken json, UserJson user)
        {
            if (json == null)
                return Enumerable.Empty<TimeEntryJson> ();
            return json.ToObject<List<TimeEntryJson>> ().Select ((te) => {
                te.UserId = user.Id.Value;
                return te;
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
