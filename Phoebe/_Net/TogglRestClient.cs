using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data;
using XPlatUtils;

namespace Toggl.Phoebe._Net
{
    public class TogglRestClient : ITogglClient
    {
        private static readonly DateTime UnixStart = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private readonly Uri v8Url;
        private readonly Uri v9Url;
        private readonly string authToken;

        public TogglRestClient (Uri url, string authToken)
        {
            v8Url = new Uri (url, "v8/");
            v9Url = new Uri (url, "v9/");
            this.authToken = authToken;
        }

        private HttpClient MakeHttpClient ()
        {
            // Cannot share HttpClient instance between threads as it might (and will) cause InvalidOperationExceptions
            // occasionally.
            var client = new HttpClient () {
                Timeout = TimeSpan.FromSeconds (10),
            };
            var headers = client.DefaultRequestHeaders;
            headers.UserAgent.Clear ();
            headers.UserAgent.Add (new ProductInfoHeaderValue (Platform.AppIdentifier, Platform.AppVersion));
            headers.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

            return client;
        }

        public async Task<T> Create<T> (T dataObject)
        where T : CommonData
        {
            var type = dataObject.GetType ();
            if (type == typeof (ClientData)) {
                return (T) (object)await CreateClient ((ClientData) (object)dataObject);
            } else if (type == typeof (ProjectData)) {
                return (T) (object)await CreateProject ((ProjectData) (object)dataObject);
            } else if (type == typeof (TaskData)) {
                return (T) (object)await CreateTask ((TaskData) (object)dataObject);
            } else if (type == typeof (TimeEntryData)) {
                return (T) (object)await CreateTimeEntry ((TimeEntryData) (object)dataObject);
            } else if (type == typeof (WorkspaceData)) {
                return (T) (object)await CreateWorkspace ((WorkspaceData) (object)dataObject);
            } else if (type == typeof (UserData)) {
                return (T) (object)await CreateUser ((UserData) (object)dataObject);
            } else if (type == typeof (TagData)) {
                return (T) (object)await CreateTag ((TagData) (object)dataObject);
            } else if (type == typeof (WorkspaceUserData)) {
                return (T) (object)await CreateWorkspaceUser ((WorkspaceUserData) (object)dataObject);
            } else if (type == typeof (ProjectUserData)) {
                return (T) (object)await CreateProjectUser ((ProjectUserData) (object)dataObject);
            } else {
                throw new NotSupportedException (String.Format ("Creating of {0} is not supported.", type));
            }
        }

        public async Task<T> Get<T> (long id)
        where T : CommonData
        {
            var type = typeof (T);
            if (type == typeof (ClientData)) {
                return (T) (object)await GetClient (id);
            } else if (type == typeof (ProjectData)) {
                return (T) (object)await GetProject (id);
            } else if (type == typeof (TaskData)) {
                return (T) (object)await GetTask (id);
            } else if (type == typeof (TimeEntryData)) {
                return (T) (object)await GetTimeEntry (id);
            } else if (type == typeof (WorkspaceData)) {
                return (T) (object)await GetWorkspace (id);
            } else if (type == typeof (UserData)) {
                return (T) (object)await GetUser (id);
            } else {
                throw new NotSupportedException (String.Format ("Fetching of {0} is not supported.", type));
            }
        }

        public async Task<List<T>> List<T> ()
        where T : CommonData
        {
            var type = typeof (T);
            if (type == typeof (ClientData)) {
                return (List<T>) (object)await ListClients ();
            } else if (type == typeof (TimeEntryData)) {
                return (List<T>) (object)await ListTimeEntries ();
            } else if (type == typeof (WorkspaceData)) {
                return (List<T>) (object)await ListWorkspaces ();
            } else {
                throw new NotSupportedException (String.Format ("Listing of {0} is not supported.", type));
            }
        }

        public async Task<T> Update<T> (T dataObject)
        where T : CommonData
        {
            var type = dataObject.GetType ();
            if (type == typeof (ClientData)) {
                return (T) (object)await UpdateClient ((ClientData) (object)dataObject);
            } else if (type == typeof (ProjectData)) {
                return (T) (object)await UpdateProject ((ProjectData) (object)dataObject);
            } else if (type == typeof (TaskData)) {
                return (T) (object)await UpdateTask ((TaskData) (object)dataObject);
            } else if (type == typeof (TimeEntryData)) {
                return (T) (object)await UpdateTimeEntry ((TimeEntryData) (object)dataObject);
            } else if (type == typeof (WorkspaceData)) {
                return (T) (object)await UpdateWorkspace ((WorkspaceData) (object)dataObject);
            } else if (type == typeof (UserData)) {
                return (T) (object)await UpdateUser ((UserData) (object)dataObject);
            } else if (type == typeof (TagData)) {
                return (T) (object)await UpdateTag ((TagData) (object)dataObject);
            } else if (type == typeof (WorkspaceUserData)) {
                return (T) (object)await UpdateWorkspaceUser ((WorkspaceUserData) (object)dataObject);
            } else if (type == typeof (ProjectUserData)) {
                return (T) (object)await UpdateProjectUser ((ProjectUserData) (object)dataObject);
            } else {
                throw new NotSupportedException (String.Format ("Updating of {0} is not supported.", type));
            }
        }

        public async Task Delete<T> (T dataObject)
        where T : CommonData
        {
            var type = dataObject.GetType ();
            if (type == typeof (ClientData)) {
                await DeleteClient ((ClientData) (object)dataObject);
            } else if (type == typeof (ProjectData)) {
                await DeleteProject ((ProjectData) (object)dataObject);
            } else if (type == typeof (TaskData)) {
                await DeleteTask ((TaskData) (object)dataObject);
            } else if (type == typeof (TimeEntryData)) {
                await DeleteTimeEntry ((TimeEntryData) (object)dataObject);
            } else if (type == typeof (TagData)) {
                await DeleteTag ((TagData) (object)dataObject);
            } else if (type == typeof (WorkspaceUserData)) {
                await DeleteWorkspaceUser ((WorkspaceUserData) (object)dataObject);
            } else if (type == typeof (ProjectUserData)) {
                await DeleteProjectUser ((ProjectUserData) (object)dataObject);
            } else {
                throw new NotSupportedException (String.Format ("Deleting of {0} is not supported.", type));
            }
        }

        public async Task Delete<T> (IEnumerable<T> dataObjects)
        where T : CommonData
        {
            var type = typeof (T);
            if (type == typeof (ClientData)) {
                await Task.WhenAll (dataObjects.Select ((object json) => DeleteClient ((ClientData)json)));
            } else if (type == typeof (ProjectData)) {
                await DeleteProjects (dataObjects as IEnumerable<ProjectData>);
            } else if (type == typeof (TaskData)) {
                await DeleteTasks (dataObjects as IEnumerable<TaskData>);
            } else if (type == typeof (TimeEntryData)) {
                await Task.WhenAll (dataObjects.Select ((object json) => DeleteTimeEntry ((TimeEntryData)json)));
            } else if (type == typeof (CommonData)) {
                // Cannot use LINQ due to AOT failure when using lambdas that use generic method calls inside them.
                var tasks = new List<Task> ();
                foreach (var data in dataObjects) {
                    tasks.Add (Delete (data));
                }
                await Task.WhenAll (tasks);
            } else {
                throw new NotSupportedException (String.Format ("Batch deleting of {0} is not supported.", type));
            }
        }

        private string StringifyData (CommonData dataObject)
        {
            var type = dataObject.GetType ();

            string dataKey;
            if (type == typeof (TimeEntryData)) {
                dataKey = "time_entry";
            } else if (type == typeof (ProjectData)) {
                dataKey = "project";
            } else if (type == typeof (ClientData)) {
                dataKey = "client";
            } else if (type == typeof (TaskData)) {
                dataKey = "task";
            } else if (type == typeof (WorkspaceData)) {
                dataKey = "workspace";
            } else if (type == typeof (UserData)) {
                dataKey = "user";
            } else if (type == typeof (TagData)) {
                dataKey = "tag";
            } else if (type == typeof (WorkspaceUserData)) {
                dataKey = "workspace_user";
            } else if (type == typeof (ProjectUserData)) {
                dataKey = "project_user";
            } else {
                throw new ArgumentException (String.Format ("Don't know how to handle JSON object of type {0}.", type), "jsonObject");
            }

            var json = new JObject ();
            json.Add (dataKey, JObject.FromObject (dataObject));
            return json.ToString (Formatting.None);
        }

        private HttpRequestMessage SetupRequest (HttpRequestMessage req)
        {
            if (authToken != null) {
                req.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                        Convert.ToBase64String (Encoding.ASCII.GetBytes (
                                                    string.Format ("{0}:api_token", authToken))));
            }
            return req;
        }

        private async Task PrepareResponse (HttpResponseMessage resp, TimeSpan requestTime)
        {
            ServiceContainer.Resolve<MessageBus> ().Send (new TogglHttpResponseMessage (this, resp, requestTime));
            if (!resp.IsSuccessStatusCode) {
                string content = string.Empty;
                if (resp.Content != null) {
                    content = await resp.Content.ReadAsStringAsync ().ConfigureAwait (false);
                }
                throw new UnsuccessfulRequestException (resp.StatusCode, resp.ReasonPhrase + ":" + content);
            }
        }

        private async Task<HttpResponseMessage> SendAsync (HttpRequestMessage httpReq, CancellationToken cancellationToken = CancellationToken.None)
        {
            using (var httpClient = MakeHttpClient ()) {
                var reqTimer = Stopwatch.StartNew ();
                var httpResp = await httpClient.SendAsync (httpReq, cancellationToken)
                               .ConfigureAwait (false);
                reqTimer.Stop ();
                await PrepareResponse (httpResp, reqTimer.Elapsed);
                return httpResp;
            }
        }

        private async Task<T> CreateObject<T> (Uri url, T dataObject)
        where T : CommonData, new()
        {
            var json = StringifyData (dataObject);
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            });
            var httpResp = await SendAsync (httpReq)
                           .ConfigureAwait (false);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                           .ConfigureAwait (false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);
            return wrap.Data;
        }

        private async Task<T> GetObject<T> (Uri url)
        where T : CommonData, new()
        {
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            });
            var httpResp = await SendAsync (httpReq)
                           .ConfigureAwait (false);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                           .ConfigureAwait (false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);
            return wrap.Data;
        }

        private async Task<T> UpdateObject<T> (Uri url, T dataObject)
        where T : CommonData, new()
        {
            var json = StringifyData (dataObject);
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Put,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            });
            var httpResp = await SendAsync (httpReq)
                           .ConfigureAwait (false);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                           .ConfigureAwait (false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<T>> (respData);
            return wrap.Data;
        }

        private Task<List<T>> ListObjects<T> (Uri url)
        where T : CommonData, new()
        {
            return ListObjects<T> (url, CancellationToken.None);
        }

        private async Task<List<T>> ListObjects<T> (Uri url, CancellationToken cancellationToken)
        where T : CommonData, new()
        {
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            });

            var httpResp = await SendAsync (httpReq, cancellationToken)
                           .ConfigureAwait (false);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                           .ConfigureAwait (false);
            return JsonConvert.DeserializeObject<List<T>> (respData) ?? new List<T> (0);
        }

        private async Task DeleteObject (Uri url)
        {
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Delete,
                RequestUri = url,
            });
            await SendAsync (httpReq).ConfigureAwait (false);
        }

        private Task DeleteObjects (Uri url)
        {
            return DeleteObject (url);
        }

        #region Client methods

        public Task<ClientData> CreateClient (ClientData dataObject)
        {
            var url = new Uri (v8Url, "clients");
            return CreateObject (url, dataObject);
        }

        public Task<ClientData> GetClient (long id)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", id));
            return GetObject<ClientData> (url);
        }

        public Task<List<ClientData>> ListClients ()
        {
            var url = new Uri (v8Url, "clients");
            return ListObjects<ClientData> (url);
        }

        public Task<List<ClientData>> ListWorkspaceClients (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/clients", workspaceId));
            return ListObjects<ClientData> (url);
        }

        public Task<ClientData> UpdateClient (ClientData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", dataObject.RemoteId.Value));
            return UpdateObject (url, dataObject);
        }

        public Task DeleteClient (ClientData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("clients/{0}", dataObject.RemoteId.Value));
            return DeleteObject (url);
        }

        #endregion

        #region Project methods

        public Task<ProjectData> CreateProject (ProjectData dataObject)
        {
            var url = new Uri (v8Url, "projects");
            return CreateObject (url, dataObject);
        }

        public Task<ProjectData> GetProject (long id)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", id));
            return GetObject<ProjectData> (url);
        }

        public Task<List<ProjectData>> ListWorkspaceProjects (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/projects", workspaceId));
            return ListObjects<ProjectData> (url);
        }

        public Task<List<WorkspaceUserData>> ListWorkspaceUsers (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/workspace_users", workspaceId));
            return ListObjects<WorkspaceUserData> (url);
        }

        public Task<ProjectData> UpdateProject (ProjectData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", dataObject.RemoteId));
            return UpdateObject (url, dataObject);
        }

        public Task DeleteProject (ProjectData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}", dataObject.RemoteId.Value));
            return DeleteObject (url);
        }

        public Task DeleteProjects (IEnumerable<ProjectData> dataObjects)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}",
                               String.Join (",", dataObjects.Select (model => model.RemoteId.Value.ToString ()))));
            return DeleteObjects (url);
        }

        #endregion

        #region Task methods

        public Task<TaskData> CreateTask (TaskData dataObject)
        {
            var url = new Uri (v8Url, "tasks");
            return CreateObject (url, dataObject);
        }

        public Task<TaskData> GetTask (long id)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", id));
            return GetObject<TaskData> (url);
        }

        public Task<List<TaskData>> ListProjectTasks (long projectId)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}/tasks", projectId));
            return ListObjects<TaskData> (url);
        }

        public Task<List<ProjectUserData>> ListProjectUsers (long projectId)
        {
            var url = new Uri (v8Url, String.Format ("projects/{0}/project_users", projectId));
            return ListObjects<ProjectUserData> (url);
        }

        public Task<List<TaskData>> ListWorkspaceTasks (long workspaceId)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/tasks", workspaceId));
            return ListObjects<TaskData> (url);
        }

        public Task<TaskData> UpdateTask (TaskData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", dataObject.RemoteId));
            return UpdateObject (url, dataObject);
        }

        public Task DeleteTask (TaskData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}", dataObject.RemoteId.Value));
            return DeleteObject (url);
        }

        public Task DeleteTasks (IEnumerable<TaskData> dataObjects)
        {
            var url = new Uri (v8Url, String.Format ("tasks/{0}",
                               String.Join (",", dataObjects.Select (data => data.RemoteId.Value.ToString ()))));
            return DeleteObjects (url);
        }

        #endregion

        #region Time entry methods

        public Task<TimeEntryData> CreateTimeEntry (TimeEntryData dataObject)
        {
            var url = new Uri (v8Url, "time_entries");
            dataObject.CreatedWith = Platform.DefaultCreatedWith;
            return CreateObject (url, dataObject);
        }

        public Task<TimeEntryData> GetTimeEntry (long id)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", id));
            return GetObject<TimeEntryData> (url);
        }

        public Task<List<TimeEntryData>> ListTimeEntries ()
        {
            var url = new Uri (v8Url, "time_entries");
            return ListObjects<TimeEntryData> (url);
        }

        public Task<List<TimeEntryData>> ListTimeEntries (DateTime start, DateTime end)
        {
            var url = new Uri (v8Url,
                               String.Format ("time_entries?start_date={0}&end_date={1}",
                                              WebUtility.UrlEncode (start.ToUtc ().ToString ("o")),
                                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o"))));
            return ListObjects<TimeEntryData> (url);
        }

        public Task<List<TimeEntryData>> ListTimeEntries (DateTime end, int days)
        {
            var url = new Uri (v8Url,
                               String.Format ("time_entries?end_date={0}&num_of_days={1}",
                                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o")),
                                              days));
            return ListObjects<TimeEntryData> (url);
        }

        public Task<List<TimeEntryData>> ListTimeEntries (DateTime start, DateTime end, CancellationToken cancellationToken)
        {
            var url = new Uri (v8Url,
                               String.Format ("time_entries?start_date={0}&end_date={1}",
                                              WebUtility.UrlEncode (start.ToUtc ().ToString ("o")),
                                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o"))));
            return ListObjects<TimeEntryData> (url, cancellationToken);
        }

        public Task<List<TimeEntryData>> ListTimeEntries (DateTime end, int days, CancellationToken cancellationToken)
        {
            var url = new Uri (v8Url,
                               String.Format ("time_entries?end_date={0}&num_of_days={1}",
                                              WebUtility.UrlEncode (end.ToUtc ().ToString ("o")),
                                              days));
            return ListObjects<TimeEntryData> (url, cancellationToken);
        }

        public Task<TimeEntryData> UpdateTimeEntry (TimeEntryData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", dataObject.RemoteId.Value.ToString ()));
            return UpdateObject (url, dataObject);
        }

        public Task DeleteTimeEntry (TimeEntryData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("time_entries/{0}", dataObject.RemoteId.Value.ToString ()));
            return DeleteObject (url);
        }

        #endregion

        #region Workspace methods

        public Task<WorkspaceData> CreateWorkspace (WorkspaceData dataObject)
        {
            var url = new Uri (v8Url, "workspaces");
            return CreateObject (url, dataObject);
        }

        public Task<WorkspaceData> GetWorkspace (long id)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}", id.ToString ()));
            return GetObject<WorkspaceData> (url);
        }

        public Task<List<WorkspaceData>> ListWorkspaces ()
        {
            var url = new Uri (v8Url, "workspaces");
            return ListObjects<WorkspaceData> (url);
        }

        public Task<WorkspaceData> UpdateWorkspace (WorkspaceData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}", dataObject.RemoteId.Value));
            return UpdateObject (url, dataObject);
        }

        #endregion

        #region Tag methods

        public Task<TagData> CreateTag (TagData dataObject)
        {
            var url = new Uri (v8Url, "tags");
            return CreateObject (url, dataObject);
        }

        public Task<TagData> UpdateTag (TagData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("tags/{0}", dataObject.RemoteId.Value));
            return UpdateObject (url, dataObject);
        }

        public Task DeleteTag (TagData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("tags/{0}", dataObject.RemoteId.Value));
            return DeleteObject (url);
        }

        #endregion

        #region Workspace user methods

        public async Task<WorkspaceUserData> CreateWorkspaceUser (WorkspaceUserData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("workspaces/{0}/invite", dataObject.WorkspaceId));

            var json = JsonConvert.SerializeObject (new {
                emails = new string[] { dataObject.Email },
            });
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            });
            var httpResp = await SendAsync (httpReq)
                           .ConfigureAwait (false);

            var wrap = JObject.Parse (await httpResp.Content.ReadAsStringAsync ()
                                      .ConfigureAwait (false));
            var data = wrap ["data"] [0].ToObject<WorkspaceUserData> ();
            return data;
        }

        public Task<WorkspaceUserData> UpdateWorkspaceUser (WorkspaceUserData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("workspace_users/{0}", dataObject.RemoteId.Value));
            return UpdateObject (url, dataObject);
        }

        public Task DeleteWorkspaceUser (WorkspaceUserData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("workspace_users/{0}", dataObject.RemoteId.Value));
            return DeleteObject (url);
        }

        #endregion

        #region Project user methods

        public Task<ProjectUserData> CreateProjectUser (ProjectUserData dataObject)
        {
            var url = new Uri (v8Url, "project_users");
            return CreateObject (url, dataObject);
        }

        public Task<ProjectUserData> UpdateProjectUser (ProjectUserData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("project_users/{0}", dataObject.RemoteId.Value));
            return UpdateObject (url, dataObject);
        }

        public Task DeleteProjectUser (ProjectUserData dataObject)
        {
            var url = new Uri (v8Url, String.Format ("project_users/{0}", dataObject.RemoteId.Value));
            return DeleteObject (url);
        }

        #endregion

        #region User methods

        public Task<UserData> CreateUser (UserData dataObject)
        {
            var url = new Uri (v8Url, dataObject.GoogleAccessToken != null ? "signups?app_name=toggl_mobile" : "signups");
            dataObject.CreatedWith = Platform.DefaultCreatedWith;
            return CreateObject (url, dataObject);
        }

        public Task<UserData> GetUser (long id)
        {
            var url = new Uri (v8Url, "me");
            return GetObject<UserData> (url);
        }

        public async Task<UserData> GetUser (string username, string password)
        {
            var url = new Uri (v8Url, "me");

            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                    Convert.ToBase64String (Encoding.ASCII.GetBytes (
                                                string.Format ("{0}:{1}", username, password))));
            var httpResp = await SendAsync (httpReq).ConfigureAwait (false);
            var respData = await httpResp.Content.ReadAsStringAsync ().ConfigureAwait (false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<UserData>> (respData);

            return wrap.Data;
        }

        public async Task<UserData> GetUser (string googleAccessToken)
        {
            var url = new Uri (v8Url, "me?app_name=toggl_mobile");
            var httpReq = new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            };
            httpReq.Headers.Authorization = new AuthenticationHeaderValue ("Basic",
                    Convert.ToBase64String (Encoding.ASCII.GetBytes (
                                                string.Format ("{0}:{1}", googleAccessToken, "google_access_token"))));
            var httpResp = await SendAsync (httpReq).ConfigureAwait (false);
            var respData = await httpResp.Content.ReadAsStringAsync ().ConfigureAwait (false);
            var wrap = JsonConvert.DeserializeObject<Wrapper<UserData>> (respData);
            return wrap.Data;
        }

        public Task<UserData> UpdateUser (UserData dataObject)
        {
            var url = new Uri (v8Url, "me");
            return UpdateObject (url, dataObject);
        }

        #endregion

        public async Task<UserRelatedData> GetChanges (DateTime? since)
        {
            since = since.ToUtc ();
            var relUrl = "me?with_related_data=true";
            if (since.HasValue) {
                relUrl = String.Format ("{0}&since={1}", relUrl, (long) (since.Value - UnixStart).TotalSeconds);
            }
            var url = new Uri (v8Url, relUrl);

            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Get,
                RequestUri = url,
            });
            var httpResp = await SendAsync (httpReq)
                           .ConfigureAwait (false);

            var respData = await httpResp.Content.ReadAsStringAsync ()
                           .ConfigureAwait (false);
            var json = JObject.Parse (respData);

            var user = json ["data"].ToObject<UserData> ();
            return new UserRelatedData () {
                Timestamp = UnixStart + TimeSpan.FromSeconds ((long)json ["since"]),
                User = user,
                Workspaces = GetChangesObjects<WorkspaceData> (json ["data"] ["workspaces"]),
                Tags = GetChangesObjects<TagData> (json ["data"] ["tags"]),
                Clients = GetChangesObjects<ClientData> (json ["data"] ["clients"]),
                Projects = GetChangesObjects<ProjectData> (json ["data"] ["projects"]),
                Tasks = GetChangesObjects<TaskData> (json ["data"] ["tasks"]),
                TimeEntries = GetChangesTimeEntryObjects (json ["data"] ["time_entries"], user),
            };
        }

        private IEnumerable<T> GetChangesObjects<T> (JToken json)
        where T : CommonData, new()
        {
            if (json == null) {
                return Enumerable.Empty<T> ();
            }
            return json.ToObject<List<T>> ();
        }

        private IEnumerable<TimeEntryData> GetChangesTimeEntryObjects (JToken json, UserData user)
        {
            if (json == null) {
                return Enumerable.Empty<TimeEntryData> ();
            }
            return json.ToObject<List<TimeEntryData>> ().Select ((te) => {
                te.UserId = user.Id.Value;
                return te;
            });
        }

        public async Task CreateFeedback (FeedbackJson dataObject)
        {
            var url = new Uri (v8Url, "feedback");

            dataObject.AppVersion = String.Format ("{0}/{1}", Platform.AppIdentifier, Platform.AppVersion);
            dataObject.Timestamp = Time.Now;

            var json = JsonConvert.SerializeObject (dataObject);
            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json"),
            });
            await SendAsync (httpReq).ConfigureAwait (false);
        }

        public async Task CreateExperimentAction (ActionJson dataObject)
        {
            var url = new Uri (v9Url, "obm/actions");
            var json = JsonConvert.SerializeObject (dataObject);

            var httpReq = SetupRequest (new HttpRequestMessage () {
                Method = HttpMethod.Post,
                RequestUri = url,
                Content = new StringContent (json, Encoding.UTF8, "application/json")
            });
            var response = await SendAsync (httpReq).ConfigureAwait (false);
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
