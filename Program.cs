using System;
using System.Linq;
using TogglDoodle.Models;
using SQLite;

namespace TogglDoodle
{
    class MainClass
    {
        private static void CreateTables (SQLiteConnection db)
        {
            var modelType = typeof(Model);
            // Auto-discover models in single assembly namespace
            var modelSubtypes =
                from t in modelType.Assembly.GetTypes ()
                            where t.Namespace == modelType.Namespace && t.IsSubclassOf (modelType)
                            select t;
            foreach (var t in modelSubtypes) {
                db.CreateTable (t);
            }
        }

        public static void Main (string[] args)
        {
            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggldoodle.db");
            Console.WriteLine ("Using SQLite file: {0}", path);

            var db = new SQLiteConnection (path);
            CreateTables (db);

//            var ws = Model.GetShared (new WorkspaceModel () {
//                Id = WorkspaceModel.NextId,
//                Name = "Test workspace",
//                DefaultCurrency = "EUR",
//                DefaultHourlyRate = 70.5m,
//                IsAdmin = true,
//                IsPremium = true,
//            });
//
//            var te = Model.GetShared (new TimeEntryModel () {
//                Id = TimeEntryModel.NextId,
//                Workspace = ws,
//                Billable = true,
//                CreatedWith = "Me",
//                Description = "Testing...",
//                Start = DateTime.UtcNow,
//            });
//
//            db.Insert (ws);
//            db.Insert (te);

//            var ws = Model.GetShared (db.Get<WorkspaceModel> (1));
//            var tes = db.Table<TimeEntryModel> ().Where (ws.TimeEntries);
//
//            Console.WriteLine ("Workspace: {0}", ws.Name);
//            Console.WriteLine ("TimeEntries: {0}", tes.Count ());
//            Console.WriteLine ("Workspace is same: {0}", tes.First ().Workspace == ws);

            Console.WriteLine ("Hello World!");
        }
    }
}
