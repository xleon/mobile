using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using Toggl.Phoebe;
using XPlatUtils;

namespace Toggl.Joey.Widget
{
    [Service(Exported = false, Permission = Android.Manifest.Permission.BindRemoteviews)]
    public class RemotesViewsFactoryService : RemoteViewsService
    {
        public override IRemoteViewsFactory OnGetViewFactory(Intent intent)
        {
            return new RemotesViewsFactory(ApplicationContext);
        }
    }

    public class RemotesViewsFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
    {
        private Context context;

        public RemotesViewsFactory(Context ctx)
        {
            context = ctx;
        }

        public long GetItemId(int position)
        {
            return position;
        }

        public void OnCreate()
        {
            //itemList.AddRange (UpdateService.LastEntries);
        }

        public void OnDestroy()
        {
        }

        public RemoteViews GetViewAt(int position)
        {
            var remoteView = new RemoteViews(context.PackageName, Resource.Layout.widget_list_item);
            /*
            var rowData = itemList [position];

            // set if is running
            if (rowData.IsRunning) {
                remoteView.SetImageViewResource (Resource.Id.WidgetContinueImageButton, Resource.Drawable.IcWidgetStop);
            } else {
                remoteView.SetImageViewResource (Resource.Id.WidgetContinueImageButton, Resource.Drawable.IcWidgetPlay);
            }

            // set color
            remoteView.SetInt (Resource.Id.WidgetColorView, "setColorFilter", Color.ParseColor (rowData.Color));
            remoteView.SetOnClickFillInIntent (Resource.Id.WidgetContinueImageButton, ConstructFillIntent (rowData));

            // set content
            remoteView.SetTextViewText (
                Resource.Id.DescriptionTextView,
                String.IsNullOrWhiteSpace (rowData.Description) ? context.Resources.GetString (Resource.String.RunningWidgetNoDescription) : rowData.Description);
            remoteView.SetTextViewText (
                Resource.Id.ProjectTextView,
                String.IsNullOrWhiteSpace (rowData.ProjectName) ? context.Resources.GetString (Resource.String.RunningWidgetNoProject) : rowData.ProjectName);
            remoteView.SetTextViewText (Resource.Id.DurationTextView, rowData.TimeValue);
            */
            return remoteView;
        }
        /*
        private Intent ConstructFillIntent (WidgetSyncManager.WidgetEntryData entryData)
        {
            var intent = new Intent ();

            if (entryData.IsRunning) {
                intent.SetAction (WidgetProvider.StartStopAction);
            } else {
                intent.SetAction (WidgetProvider.ContiueAction);
                intent.PutExtra (WidgetProvider.TimeEntryIdParameter, entryData.Id);
            }

            return intent;
        }
        */

        public void OnDataSetChanged()
        {
        }

        public int Count
        {
            get
            {
                return 0;
            }
        }

        public bool HasStableIds
        {
            get
            {
                return true;
            }
        }

        public RemoteViews LoadingView
        {
            get
            {
                return (RemoteViews) null;
            }
        }

        public int ViewTypeCount
        {
            get
            {
                return 1;
            }
        }
    }
}