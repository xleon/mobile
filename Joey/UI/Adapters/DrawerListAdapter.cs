using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.UI.Views;

namespace Toggl.Joey.UI.Adapters
{
    class DrawerListAdapter : BaseAdapter
    {
        protected static readonly int ViewTypeDrawerHeader = 0;
        protected static readonly int ViewTypeDrawerItem = 1;
        public static readonly int TimerPageId = 0;
        public static readonly int ReportsPageId = 1;
        public static readonly int SettingsPageId = 2;
        public static readonly int LogoutPageId = 3;
        private List<DrawerItem> rowItems = new List<DrawerItem> ();
        private AuthManager authManager = null;

        public DrawerListAdapter ()
        {
            rowItems.Insert (TimerPageId, new DrawerItem () {
                TextResId = Resource.String.MainDrawerTimer,
                ImageResId = Resource.Drawable.IcTimerGray
            });
            rowItems.Insert (ReportsPageId, new DrawerItem () {
                TextResId = Resource.String.MainDrawerReports,
                ImageResId = Resource.Drawable.IcReportsGray
            });
            rowItems.Insert (SettingsPageId, new DrawerItem () {
                TextResId = Resource.String.MainDrawerSettings,
                ImageResId = Resource.Drawable.IcSettingsGray
            });
            rowItems.Insert (LogoutPageId, new DrawerItem () {
                TextResId = Resource.String.MainDrawerLogout,
                ImageResId = Resource.Drawable.IcLogoutGray
            });
            authManager = ServiceContainer.Resolve<AuthManager> ();
        }

        public override int ViewTypeCount {
            get {
                return 2;
            }
        }

        public override int GetItemViewType (int position)
        {
            if (position == 0) {
                return ViewTypeDrawerHeader;
            } else {
                return ViewTypeDrawerItem;
            }
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null) {
                if (GetItemViewType (position) == ViewTypeDrawerHeader) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.MainDrawerListHeader, parent, false);
            
                    view.FindViewById<ProfileImageView> (Resource.Id.ProfileImageViewIcon).ImageUrl = authManager.User.ImageUrl;
                    view.FindViewById<TextView> (Resource.Id.TextViewTitle).Text = authManager.User.Name;

                } else if (GetItemViewType (position) == ViewTypeDrawerItem) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                        Resource.Layout.MainDrawerListItem, parent, false);


                }
            }

            if (GetItemViewType (position) == ViewTypeDrawerItem) {
                DrawerItem item = GetDrawerItem (position);
                view.FindViewById<ImageView> (Resource.Id.ImageViewIcon).SetImageResource (item.ImageResId);
                view.FindViewById<TextView> (Resource.Id.TextViewTitle).SetText (item.TextResId);
            }


            return view;
        }

        public override int Count {
            get{ return rowItems.Count + 1; } // + 1 is for header
        }

        public override Java.Lang.Object GetItem (int position)
        {
            throw new NotImplementedException ("User method GetDrawerItem instead!");
        }

        private DrawerItem GetDrawerItem (int position)
        {
            return rowItems [position - 1]; //Header is 0
        }

        public override long GetItemId (int position)
        {
            if (GetItemViewType (position) == ViewTypeDrawerHeader) {
                return -1;
            } else {
                return rowItems.IndexOf (GetDrawerItem (position));
            }
        }

        private struct DrawerItem
        {
            public int TextResId;
            public int ImageResId;
        }
    }
}

