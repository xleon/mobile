using System;
using System.Linq;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Chandler
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggldoodle.db");
            Console.WriteLine ("Using SQLite file: {0}", path);
            ServiceContainer.Register<MessageBus> ();
            ServiceContainer.Register<IPlatformInfo> (() => new ConsolePlatformInfo ());
            ServiceContainer.Register<ISettingsStore> (() => new TempSettingsStore ());
            ServiceContainer.Register<IModelStore> (new SQLiteModelStore (path));
            ServiceContainer.Register<ITogglClient> (() => new TogglRestClient (new Uri ("https://next.toggl.com/api/")));
            ServiceContainer.Register<AuthManager> ();
            ServiceContainer.Register<SyncManager> ();

//            var ws = Model.Update (new WorkspaceModel () {
//                Name = "Test workspace",
//                DefaultCurrency = "EUR",
//                DefaultRate = 70.5m,
//                IsAdmin = true,
//                IsPremium = true,
//                IsPersisted = true,
//            });
//
//            var te = Model.Update (new TimeEntryModel () {
//                Workspace = ws,
//                IsBillable = true,
//                CreatedWith = "Me",
//                Description = "Testing...",
//                StartTime = DateTime.UtcNow,
//                IsPersisted = true,
//            });
//
//            Model.Store.Commit ();
//
//            var ws = Model.Get<WorkspaceModel> (1);
//            var tes = ws.TimeEntries;
//
//            Console.WriteLine ("Workspace: {0}", ws.Name);
//            Console.WriteLine ("TimeEntries: {0}", tes.Count ());
//            Console.WriteLine ("Workspace is same: {0}", tes.First ().Workspace == ws);

            Console.WriteLine ("Hello World!");
        }

        private class TempSettingsStore : ISettingsStore
        {
            public Guid? UserId { get; set; }

            public string ApiToken { get; set; }

            public DateTime? SyncLastRun { get; set; }
        }

        private class ConsolePlatformInfo : IPlatformInfo
        {
            public string AppIdentifier {
                get { return "Chandler"; }
            }

            public string AppVersion {
                get { return GetType ().Assembly.GetName ().Version.ToString (); }
            }
        }
    }
}
