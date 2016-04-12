using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Toggl.Phoebe;
using XPlatUtils;

namespace Toggl.Joey.Widget
{
    [Service (Exported = false, Permission = Android.Manifest.Permission.BindRemoteviews)]
    public class RemotesViewsFactoryService : RemoteViewsService
    {
        public override IRemoteViewsFactory OnGetViewFactory (Intent intent)
        {
            return new RemotesViewsFactory (ApplicationContext);
        }
    }

    public class RemotesViewsFactory : Java.Lang.Object, RemoteViewsService.IRemoteViewsFactory
    {
        private readonly List<WidgetSyncManager.WidgetEntryData> itemList = new  List<WidgetSyncManager.WidgetEntryData> ();
        private Context context;

        private IWidgetUpdateService widgetUpdateService;

        private IWidgetUpdateService UpdateService
        {
            get {
                if (widgetUpdateService == null) {
                    widgetUpdateService = ServiceContainer.Resolve<IWidgetUpdateService> ();
                }
                return widgetUpdateService;
            }
        }

        // Explanation of native constructor
        // http://stackoverflow.com/questions/10593022/monodroid-error-when-calling-constructor-of-custom-view-twodscrollview/10603714#10603714
        public RemotesViewsFactory (IntPtr a, Android.Runtime.JniHandleOwnership b) : base (a, b)
        {
        }

        public RemotesViewsFactory (Context ctx)
        {
            context = ctx;
        }

        public long GetItemId (int position)
        {
            return position;
        }

        public void OnCreate ()
        {
            itemList.AddRange (UpdateService.LastEntries);
        }

        public void OnDestroy ()
        {
        }

        public RemoteViews GetViewAt (int position)
        {
            var remoteView = new RemoteViews (context.PackageName, Resource.Layout.widget_list_item);
            var rowData = itemList [position];

            // set if is running
            if (rowData.IsRunning) {
                remoteView.SetImageViewResource (Resource.Id.WidgetContinueImageButton, Resource.Drawable.IcWidgetStop);
                remoteView.SetViewVisibility (Resource.Id.DurationChronometer, Android.Views.ViewStates.Visible);
                remoteView.SetViewVisibility (Resource.Id.DurationTextView, Android.Views.ViewStates.Gone);
            } else {
                remoteView.SetImageViewResource (Resource.Id.WidgetContinueImageButton, Resource.Drawable.IcWidgetPlay);
                remoteView.SetViewVisibility (Resource.Id.DurationChronometer, Android.Views.ViewStates.Gone);
                remoteView.SetViewVisibility (Resource.Id.DurationTextView, Android.Views.ViewStates.Visible);
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

            var time = (long)rowData.Duration.TotalMilliseconds;

            // Format chronometer correctly.
            string format = "00:%s";
            if (time >= 3600000 && time < 36000000) {
                format = "0%s";
            } else if (time >= 36000000) {
                format = "%s";
            }
            var baseTime = SystemClock.ElapsedRealtime ();
            remoteView.SetChronometer (Resource.Id.DurationChronometer, baseTime - (long)rowData.Duration.TotalMilliseconds, format, true);


            return remoteView;
        }

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

        public void OnDataSetChanged ()
        {
            itemList.Clear ();
            itemList.AddRange (UpdateService.LastEntries);
        }

        public int Count
        {
            get {
                return itemList.Count;
            }
        }

        public bool HasStableIds
        {
            get {
                return true;
            }
        }

        public RemoteViews LoadingView
        {
            get {
                return (RemoteViews) null;
            }
        }

        public int ViewTypeCount
        {
            get {
                return 1;
            }
        }
    }
}