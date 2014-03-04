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
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Adapters;
using DrawerLayout = Android.Support.V4.Widget.DrawerLayout;

namespace Toggl.Joey.UI.Activities
{
    public abstract class BaseDrawerActivity : BaseActivity
    {
        protected Logger log;
        protected DrawerLayout DrawerLayout;

        protected override void OnCreate (Bundle bundle)
        {
            base.OnCreate (bundle);

            SetContentView (Resource.Layout.MainDrawerLayout);

            DrawerLayout = FindViewById<DrawerLayout> (Resource.Id.DrawerLayout);
            var drawerList = FindViewById<ListView> (Resource.Id.LeftDrawer);
            drawerList.Adapter = new DrawerListAdapter ();
            drawerList.ItemClick += OnDrawerItemClick;

            log = ServiceContainer.Resolve<Logger> ();
        }

        abstract protected void OnDrawerItemClick (object sender, ListView.ItemClickEventArgs e);
    }
}

