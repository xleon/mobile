using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Text.Format;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Components;
using Toggl.Joey.UI.Fragments;
using Fragment = Android.Support.V4.App.Fragment;
using Activity = Android.Support.V7.App.ActionBarActivity;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Label = "ProjectsActivity")]
    public class ProjectsActivity : Activity
    {

        protected override void OnCreate (Bundle bundle)
        {
            SetTheme (Resource.Style.Theme_AppCompat_Light_NoActionBar);

            base.OnCreate (bundle);

            SetContentView (Resource.Layout.ProjectActivity);

            SupportFragmentManager.BeginTransaction ()
            .Add (Resource.Id.ProjectActivityLayout, new ProjectsFragment())
            .Commit ();

            // FragmentManager.BeginTransaction ().Add (fragment, "123");


            // Create your application here
        }
    }
}

