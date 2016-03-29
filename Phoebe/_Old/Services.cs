using System;
using Toggl.Phoebe._Helpers;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe.Data;
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
            ServiceContainer.Register<IPushClient> (() => new PushRestClient (Build.ApiUrl));
            ServiceContainer.Register<_Data.ISyncDataStore> (CreateSyncDataStore);
            ServiceContainer.Register<LogStore> ();
            ServiceContainer.Register<TimeCorrectionManager> ();
            ServiceContainer.Register<_Net.IReportsClient> (() => new _Net.ReportsRestClient (Build.ReportsApiUrl));
            ServiceContainer.Register<LoggerUserManager> ();

            var restApiUrl = Settings.IsStaging ? Build.StagingUrl : Build.ApiUrl;
            ServiceContainer.Register<_Net.ITogglClient> (() => new _Net.TogglRestClient (restApiUrl));
        }

        private static _Data.ISyncDataStore CreateSyncDataStore ()
        {
            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggl.db");
            return new _Data.SyncSqliteDataStore (path, ServiceContainer.Resolve<IPlatformUtils> ().SQLiteInfo);
        }
    }
}