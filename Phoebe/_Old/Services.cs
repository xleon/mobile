﻿using System;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using Toggl.Phoebe._Reactive;
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
            ServiceContainer.Register<Toggl.Phoebe._Data.ActiveTimeEntryManager> ();
            ServiceContainer.Register<DataCache> ();
            ServiceContainer.Register<ForeignRelationManager> ();
            ServiceContainer.Register<ISyncManager> (() => new SyncManager ());
            if (ServiceContainer.Resolve<IPlatformUtils> ().IsWidgetAvailable) {
                ServiceContainer.Register<WidgetSyncManager> (() => new WidgetSyncManager ());
            }
            ServiceContainer.Register<IPushClient> (() => new PushRestClient (Build.ApiUrl));
            ServiceContainer.Register<IDataStore> (CreateDataStore);
            ServiceContainer.Register<LogStore> ();
            ServiceContainer.Register<TimeCorrectionManager> ();

            // Core services that are most likelly to be overriden by UI code:
            var restApiUrl = ServiceContainer.Resolve<ISettingsStore> ().IsStagingMode ? Build.StagingUrl : Build.ApiUrl;
            ServiceContainer.Register<ITogglClient> (() => new TogglRestClient (restApiUrl));
            ServiceContainer.Register<IReportsClient> (() => new ReportsRestClient (Build.ReportsApiUrl));

            RegisterJsonConverters ();
            ServiceContainer.Register<LoggerUserManager> ();

            ServiceContainer.Register<ISchedulerProvider> (new DefaultSchedulerProvider ());

            // Start the Reactive chain
            SyncOutManager.Init ();
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
    }
}