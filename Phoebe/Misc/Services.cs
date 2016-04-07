using System;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe
{
    public static class Services
    {
        public static void Register()
        {
            ServiceContainer.Register<MessageBus> ();
            ServiceContainer.Register<UpgradeManger> ();
            ServiceContainer.Register<IPushClient> (() => new PushRestClient(Build.ApiUrl));
            ServiceContainer.Register(CreateSyncDataStore);
            ServiceContainer.Register<LogStore> ();
            ServiceContainer.Register<TimeCorrectionManager> ();
            ServiceContainer.Register<IReportsClient> (() => new ReportsRestClient(Build.ReportsApiUrl));

            var restApiUrl = Settings.IsStaging ? Build.StagingUrl : Build.ApiUrl;
            ServiceContainer.Register<ITogglClient> (() => new TogglRestClient(restApiUrl));
        }

        private static Data.ISyncDataStore CreateSyncDataStore()
        {
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine(folder, "toggl.db");
            return new Data.SyncSqliteDataStore(path, ServiceContainer.Resolve<IPlatformUtils> ().SQLiteInfo);
        }
    }
}