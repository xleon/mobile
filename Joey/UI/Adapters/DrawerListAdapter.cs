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

namespace Toggl.Joey.UI.Adapters
{
    class DrawerListAdapter : BaseAdapter
    {
        private JavaList<DrawerItem> rowItems = new JavaList<DrawerItem> ();

        public DrawerListAdapter()
        {
            rowItems.Add (new DrawerItem (){TextResId = Resource.String.MainDrawerTimer, ImageResId = Resource.Drawable.IcTimerGray});
            rowItems.Add (new DrawerItem (){TextResId = Resource.String.MainDrawerReports, ImageResId = Resource.Drawable.IcReportsGray});
            rowItems.Add (new DrawerItem (){TextResId = Resource.String.MainDrawerSettings, ImageResId = Resource.Drawable.IcSettingsGray});
            rowItems.Add (new DrawerItem (){TextResId = Resource.String.MainDrawerLogout, ImageResId = Resource.Drawable.IcLogoutGray});
        }

        public override int ViewTypeCount {
            get {
                return 1;
            }
        }

        public override int GetItemViewType (int position)
        {
            return 0;
        }

        public override View GetView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            if (view == null) {
                view = LayoutInflater.FromContext (parent.Context).Inflate (
                    Resource.Layout.MainDrawerListItem, parent, false);
            }
            DrawerItem item = (DrawerItem) GetItem (position);
            view.FindViewById<ImageView> (Resource.Id.Icon).SetImageResource (item.ImageResId);
            view.FindViewById<TextView> (Resource.Id.Title).SetText (item.TextResId);

            return view;
        }

        public override int Count {
            get{ return rowItems.Size(); }
        }
        public override Java.Lang.Object GetItem(int position) {
            return rowItems.Get(position);
        }
        public override long GetItemId(int position) {
            return rowItems.IndexOf(GetItem(position));
        }

        private class DrawerItem : Java.Lang.Object
        {
            public int TextResId {get; set;}
            public int ImageResId {get; set;}
        }
    }
}

