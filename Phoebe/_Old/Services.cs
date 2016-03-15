using System;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static class Services
    {
        public static void Register ()
        {
            ServiceContainer.Register<MessageBus> ();
            ServiceContainer.Register<UpgradeManger> ();
            ServiceContainer.Register<AuthManager> ();
            ServiceContainer.Register<ActiveTimeEntryManager> ();
            ServiceContainer.Register<DataCache> ();
            ServiceContainer.Register<ForeignRelationManager> ();
            ServiceContainer.Register<ISyncManager> (() => new SyncManager ());
            if (ServiceContainer.Resolve<IPlatformUtils> ().IsWidgetAvailable) {
                ServiceContainer.Register<WidgetSyncManager> (() => new WidgetSyncManager ());
            }
            ServiceContainer.Register<IPushClient> (() => new PushRestClient (Build.ApiUrl));
            // TODO RX: Remove old IDataStore
            ServiceContainer.Register<IDataStore> (CreateDataStore);
            ServiceContainer.Register<_Data.ISyncDataStore> (CreateSyncDataStore);
            ServiceContainer.Register<LogStore> ();
            ServiceContainer.Register<TimeCorrectionManager> ();

            // Core services that are most likelly to be overriden by UI code:
            var restApiUrl = ServiceContainer.Resolve<ISettingsStore> ().IsStagingMode ? Build.StagingUrl : Build.ApiUrl;
            // TODO RX: Remove old ITogglClient
            ServiceContainer.Register<ITogglClient> (() => new TogglRestClient (restApiUrl));
            ServiceContainer.Register<_Net.ITogglClient> (() => new _Net.TogglRestClient (restApiUrl));
            ServiceContainer.Register<IReportsClient> (() => new ReportsRestClient (Build.ReportsApiUrl));

            RegisterJsonConverters ();
            ServiceContainer.Register<LoggerUserManager> ();
        }

        private static void RegisterJsonConverters ()
        {
            ServiceContainer.Register<ClientJsonConverter> ();
            ServiceContainer.Register<ProjectJsonConverter> ();
            ServiceContainer.Register<ProjectUserJsonConverter> ();
            ServiceContainer.Register<TagJsonConverter> ();
            ServiceContainer.Register<TaskJsonConverter> ();
            ServiceContainer.Register<TimeEntryJsonConverter> ();
            ServiceContainer.Register<UserJsonConverter> ();
            ServiceContainer.Register<WorkspaceJsonConverter> ();
            ServiceContainer.Register<WorkspaceUserJsonConverter> ();
            ServiceContainer.Register<ReportJsonConverter> ();
        }

        private static IDataStore CreateDataStore ()
        {

            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggl.db");
            return new SqliteDataStore (path, ServiceContainer.Resolve<IPlatformUtils> ().SQLiteInfo);
        }

        private static _Data.ISyncDataStore CreateSyncDataStore ()
        {

            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggl.db");
            return new _Data.SyncSqliteDataStore (path, ServiceContainer.Resolve<IPlatformUtils> ().SQLiteInfo);
        }
    }
}