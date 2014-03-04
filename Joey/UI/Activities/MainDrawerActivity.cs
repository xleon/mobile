using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Label = "@string/EntryName",
        MainLauncher = true,
        Theme = "@style/Theme.Toggl.App")]
    public class MainDrawerActivity : BaseDrawerActivity
    {
        private static readonly string LogTag = "MainDrawerActivity";

        private TimeTrackingFragment timeTrackingFragment;
        private SettingsFragment settingsFragment;
        private Fragment currentFragment;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            timeTrackingFragment = new TimeTrackingFragment ();
            settingsFragment = new SettingsFragment ();

        }

        protected override void OnResume ()
        {
            base.OnResume ();
            OpenFragment (timeTrackingFragment);
        }

        private void OpenFragment(Fragment fragment)
        {
            var fragmentTransaction = SupportFragmentManager.BeginTransaction ();
            fragmentTransaction.Replace (Resource.Id.ContentFrame, fragment);
            fragmentTransaction.AddToBackStack (null);
            fragmentTransaction.Commit ();
        }

        protected override void OnDrawerItemClick(object sender, ListView.ItemClickEventArgs e)
        {
            log.Debug (LogTag, "Drawer item clicked " + e.Id);

            if (e.Id == DrawerListAdapter.TimerPageId) {
                OpenFragment (timeTrackingFragment);

            } else if (e.Id == DrawerListAdapter.SettingsPageId) {
                OpenFragment (settingsFragment);

            }

            DrawerLayout.CloseDrawers ();
        }
    }
}

