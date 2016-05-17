
﻿using System;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using XPlatUtils;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Fragments
{
    public class MigrationFragment : Fragment
    {
#if DEBUG // TODO: DELETE TEST CODE --------
        private static void setupV0database(IPlatformUtils xplat)
        {
        var path = DatabaseHelper.GetDatabasePath(DatabaseHelper.GetDatabaseDirectory(), 0);
            if (System.IO.File.Exists(path)) { System.IO.File.Delete(path); }

            using (var db = new SQLite.Net.SQLiteConnection(xplat.SQLiteInfo, path))
            {
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.ClientData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.ProjectData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.ProjectUserData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.TagData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.TaskData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.TimeEntryData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.TimeEntryTagData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.UserData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.WorkspaceData>();
                db.CreateTable<Phoebe.Data.Models.Old.DB_VERSION_0.WorkspaceUserData>();
            }
        }

        private static void insertIntoV0Database(IPlatformUtils xplat, params object[] objects)
        {
            var dbPath = DatabaseHelper.GetDatabasePath(DatabaseHelper.GetDatabaseDirectory(), 0);
            using (var db = new SQLite.Net.SQLiteConnection(xplat.SQLiteInfo, dbPath))
            {
                db.InsertAll(objects);
            }
        }

        public static void CreateOldDbForTesting()
        {
            var workspaceData = new Phoebe.Data.Models.Old.DB_VERSION_0.WorkspaceData
            {
                Id = Guid.NewGuid(),
                Name = "the matrix",
                BillableRatesVisibility = Phoebe.Data.Models.AccessLevel.Admin,
                DefaultCurrency = "currency",
                DefaultRate = null,
                IsPremium = true,
                LogoUrl = "http://toggl.com",
                ProjectCreationPrivileges = Phoebe.Data.Models.AccessLevel.Regular,
                RoundingMode = RoundingMode.Down,
                RoundingPercision = 1
            };

            var xplat = ServiceContainer.Resolve<IPlatformUtils>();
            setupV0database(xplat);
            insertIntoV0Database(xplat, workspaceData);
        }
#endif

        int oldVersion;
        Action<bool> completionListener;
        IPlatformUtils platformUtils;

        MigrationFragment(int oldVersion, Action<bool> completionListener)
        {
            this.oldVersion = oldVersion;
            this.completionListener = completionListener;
            this.platformUtils = ServiceContainer.Resolve<IPlatformUtils>();
        }

        public MigrationFragment(IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base(jref, xfer)
        {
        }

        public static MigrationFragment Init(int oldVersion, Action<bool> completionListener)
        {
            var fragment = new MigrationFragment(oldVersion, completionListener);
            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MigrationFragment, container, false);

            Task.Run(() =>
            {
                var success = DatabaseHelper.Migrate(
                    platformUtils.SQLiteInfo, DatabaseHelper.GetDatabaseDirectory(),
                    oldVersion, SyncSqliteDataStore.DB_VERSION, report);

#if DEBUG // TODO: DELETE TEST CODE --------
                System.Threading.Thread.Sleep(2000);
#endif

                platformUtils.DispatchOnUIThread(() => completionListener(success));

            }).ConfigureAwait(false);

            return view;
        }

        void report(float progress)
        {
            //platformUtils.DispatchOnUIThread(() =>
            //{
            //});
        }
    }
}

