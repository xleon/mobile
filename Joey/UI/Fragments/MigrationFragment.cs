using System;
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

