using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Toggl.Phoebe.Bugsnag.Data;
using Toggl.Phoebe.Bugsnag.Json;

namespace Toggl.Phoebe.Bugsnag
{
    public abstract class BugsnagClient : IDisposable
    {
        private readonly string apiKey;
        private readonly Uri baseUrl;
        private readonly UserInfo userInfo = new UserInfo ();
        private readonly NotifierInfo notifierInfo = new NotifierInfo () {
            Name = "Toggl Xamarin/.NET Bugsnag Notifier",
            Version = "1.0",
            Url = "https://github.com/toggl/mobile",
        };
        private readonly Metadata metadata = new Metadata ();
        private HttpClient httpClient;

        public BugsnagClient (string apiKey)
        {
            if (String.IsNullOrEmpty (apiKey)) {
                throw new ArgumentNullException ("apiKey");
            }

            this.apiKey = apiKey;
            this.baseUrl = new Uri ("https://notify.bugsnag.com/");
            this.httpClient = new HttpClient ();
            this.httpClient.DefaultRequestHeaders.Accept.Add (
                new MediaTypeWithQualityHeaderValue ("application/json"));
            AutoNotify = true;

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        ~BugsnagClient ()
        {
            Dispose (false);
        }

        public void Dispose ()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        protected virtual void Dispose (bool disposing)
        {
            if (disposing) {
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

                if (httpClient != null) {
                    httpClient.Dispose ();
                    httpClient = null;
                }
            }
        }

        private void OnUnobservedTaskException (object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (!AutoNotify)
                return;
            Notify (e.Exception, ErrorSeverity.Warning);
        }

        private void OnUnhandledException (object sender, UnhandledExceptionEventArgs e)
        {
            if (!AutoNotify)
                return;
            var ex = e.ExceptionObject as Exception;
            if (ex == null) {
                ex = new Exception (String.Format ("Non-exception: {0}", e.ExceptionObject));
            }
            Notify (ex, e.IsTerminating ? ErrorSeverity.Fatal : ErrorSeverity.Error);
        }

        public bool AutoNotify { get; set; }

        public string Context { get; set; }

        public string ReleaseStage { get; set; }

        public List<string> NotifyReleaseStages { get; set; }

        public List<string> Filters { get; set; }

        public List<Type> IgnoredExceptions { get; set; }

        public List<string> ProjectNamespaces { get; set; }

        public string UserId {
            get { return userInfo.Id; }
            set { userInfo.Id = value; }
        }

        public string UserEmail {
            get { return userInfo.Email; }
            set { userInfo.Email = value; }
        }

        public string UserName {
            get { return userInfo.Name; }
            set { userInfo.Name = value; }
        }

        public void SetUser (string id, string email = null, string name = null)
        {
            UserId = id;
            UserEmail = email;
            UserName = name;
        }

        public void AddToTab (string tabName, string key, object value)
        {
            metadata.AddToTab (tabName, key, value);
        }

        public void ClearTab (string tabName)
        {
            metadata.ClearTab (tabName);
        }

        protected string ApiKey {
            get { return apiKey; }
        }

        protected Uri BaseUrl {
            get { return baseUrl; }
        }

        protected NotifierInfo Notifier {
            get { return notifierInfo; }
        }

        protected HttpClient HttpClient {
            get { return httpClient; }
        }

        public void TrackUser ()
        {
            var data = JsonConvert.SerializeObject (new UserMetrics () {
                ApiKey = apiKey,
                User = userInfo,
                App = GetAppInfo (),
                System = GetSystemInfo (),
            });

            var req = new HttpRequestMessage () {
                RequestUri = new Uri (baseUrl, "metrics"),
                Method = HttpMethod.Post,
                Content = new StringContent (data, System.Text.Encoding.UTF8, "application/json"),
            };

            httpClient.SendAsync (req).ContinueWith ((t) => {
                if (t.IsFaulted) {
                    LogError (String.Format ("Failed to track user: {0}", t.Exception));
                }
            });
        }

        protected bool ShouldNotify {
            get {
                if (NotifyReleaseStages == null)
                    return true;
                return NotifyReleaseStages.Contains (ReleaseStage);
            }
        }

        public void Notify (Exception e, ErrorSeverity severity = ErrorSeverity.Error, Metadata extraMetadata = null)
        {
            if (!ShouldNotify)
                return;
            if (IgnoredExceptions != null && IgnoredExceptions.Contains (e.GetType ()))
                return;

            var md = new Metadata (metadata);
            if (extraMetadata != null) {
                md.Merge (extraMetadata);
            }

            var ev = new Event () {
                User = userInfo,
                App = GetAppInfo (),
                AppState = GetAppState (),
                System = GetSystemInfo (),
                SystemState = GetSystemState (),
                Context = Context,
                Severity = severity,
                Exceptions = ConvertExceptionTree (e),
                Metadata = md,
            };

            SendEvent (ev);
        }

        protected abstract void SendEvent (Event e);

        protected abstract ApplicationInfo GetAppInfo ();

        protected abstract ApplicationState GetAppState ();

        protected abstract SystemInfo GetSystemInfo ();

        protected abstract SystemState GetSystemState ();

        protected abstract void LogError (string msg);

        private List<ExceptionInfo> ConvertExceptionTree (Exception ex)
        {
            var list = new List<ExceptionInfo> ();

            while (ex != null) {
                list.Add (ConvertException (ex));
                ex = ex.InnerException;
            }

            return list;
        }

        private ExceptionInfo ConvertException (Exception ex)
        {
            var type = ex.GetType ();
            var trace = new StackTrace (ex, true);

            return new ExceptionInfo () {
                Name = type.Name,
                Message = ex.Message,
                Stack = trace.GetFrames ().Select ((frame) => {
                    var method = frame.GetMethod ();
                    return new StackInfo () {
                        Method = String.Format ("{0}:{1}", method.DeclaringType.Name, method.Name),
                        File = frame.GetFileName () ?? "Unknown",
                        Line = frame.GetFileLineNumber (),
                        Column = frame.GetFileColumnNumber (),
                        InProject = IsInProject (method.DeclaringType),
                    };
                }).ToList (),
            };
        }

        private bool IsInProject (Type type)
        {
            var namespaces = ProjectNamespaces;
            if (namespaces == null)
                return false;

            return namespaces.Any ((ns) => type.Namespace.StartsWith (ns));
        }
    }
}
