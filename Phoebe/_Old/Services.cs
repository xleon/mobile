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
            ServiceContainer.Register<DataCache> ();
            ServiceContainer.Register<ForeignRelationManager> ();
            ServiceContainer.Register<ISyncManager> (() => new SyncManager ());
            ServiceContainer.Register<IPushClient> (() => new PushRestClient (Build.ApiUrl));
            ServiceContainer.Register<_Data.ISyncDataStore> (CreateSyncDataStore);
            ServiceContainer.Register<LogStore> ();
            ServiceContainer.Register<TimeCorrectionManager> ();

            // Core services that are most likelly to be overriden by UI code:
            var restApiUrl = ServiceContainer.Resolve<ISettingsStore> ().IsStagingMode ? Build.StagingUrl : Build.ApiUrl;
            ServiceContainer.Register<_Net.ITogglClient> (() => new _Net.TogglRestClient (restApiUrl));
            ServiceContainer.Register<_Net.IReportsClient> (() => new _Net.ReportsRestClient (Build.ReportsApiUrl));
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