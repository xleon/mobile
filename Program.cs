using System;
using System.Linq;
using Toggl.Phoebe.Data;
using SQLite;

namespace Toggl.Phoebe
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggldoodle.db");
            Console.WriteLine ("Using SQLite file: {0}", path);
            Model.Store = new SQLiteModelStore (path);

//            var ws = Model.Update (new WorkspaceModel () {
//                Id = WorkspaceModel.NextId,
//                Name = "Test workspace",
//                DefaultCurrency = "EUR",
//                DefaultRate = 70.5m,
//                IsAdmin = true,
//                IsPremium = true,
//                IsPersisted = true,
//            });
//
//            var te = Model.Update (new TimeEntryModel () {
//                Id = TimeEntryModel.NextId,
//                Workspace = ws,
//                IsBillable = true,
//                CreatedWith = "Me",
//                Description = "Testing...",
//                StartTime = DateTime.UtcNow,
//                IsPersisted = true,
//            });
//
//            Model.Store.Commit ();

//            var ws = Model.Get<WorkspaceModel> (1);
//            var tes = ws.TimeEntries;
//
//            Console.WriteLine ("Workspace: {0}", ws.Name);
//            Console.WriteLine ("TimeEntries: {0}", tes.Count ());
//            Console.WriteLine ("Workspace is same: {0}", tes.First ().Workspace == ws);

            Console.WriteLine ("Hello World!");
        }
    }
}
