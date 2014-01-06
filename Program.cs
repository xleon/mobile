using System;
using System.Linq;
using TogglDoodle.Models;
using SQLite;

namespace TogglDoodle
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggldoodle.db");
            Console.WriteLine ("Using SQLite file: {0}", path);
            Model.Store = new ModelStore (path);

//            var ws = Model.Update (new WorkspaceModel () {
//                Id = WorkspaceModel.NextId,
//                Name = "Test workspace",
//                DefaultCurrency = "EUR",
//                DefaultHourlyRate = 70.5m,
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
//            te.IsDirty = false;
//
//            Model.Store.Commit ();

//            var ws = Model.Get<WorkspaceModel> (1);
//            var tes = db.Table<TimeEntryModel> ().Where (ws.TimeEntries);

//            Console.WriteLine ("Workspace: {0}", ws.Name);
//            Console.WriteLine ("TimeEntries: {0}", tes.Count ());
//            Console.WriteLine ("Workspace is same: {0}", tes.First ().Workspace == ws);

            Console.WriteLine ("Hello World!");
        }
    }
}
