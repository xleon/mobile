using System;
using System.Linq;
using SQLite;

namespace TogglDoodle
{
    class MainClass
    {
        private static void CreateTables (SQLiteConnection db, Type type)
        {
            // Auto-discover models in single assembly namespace
            var modelTypes =
                from t in type.Assembly.GetTypes ()
                            where t.Namespace.StartsWith (type.Namespace) && t.IsSubclassOf (typeof(Model))
                            select t;
            foreach (var t in modelTypes) {
                db.CreateTable (t);
            }
        }

        public static void Main (string[] args)
        {
            string folder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            var path = System.IO.Path.Combine (folder, "toggldoodle.db");
            Console.WriteLine ("Using SQLite file: {0}", path);

            var db = new SQLiteConnection (path);
            CreateTables (db, typeof(Models.Workspace));

//            var ws = new Models.Workspace () {
//                Id = Model.NextId<Models.Workspace> (),
//                Name = "Test workspace",
//                DefaultCurrency = "EUR",
//                DefaultHourlyRate = 70.5m,
//                IsAdmin = true,
//                IsPremium = true,
//            };
//
//            var te = new Models.TimeEntry () {
//                Id = Model.NextId<Models.TimeEntry> (),
//                WorkspaceId = ws.Id,
//                Billable = true,
//                CreatedWith = "Me",
//                Description = "Testing...",
//                Start = DateTime.UtcNow,
//            };
//
//            db.Insert (ws);
//            db.Insert (te);

//            var ws = Model.GetShared (db.Get<Models.Workspace> (1));
//            var tes = db.Table<Models.TimeEntry> ().Where (ws.TimeEntries);
//
//            Console.WriteLine ("Workspace: {0}", ws.Name);
//            Console.WriteLine ("TimeEntries: {0}", tes.Count ());

            Console.WriteLine ("Hello World!");
        }
    }
}
