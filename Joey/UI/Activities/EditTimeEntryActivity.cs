using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Android.OS;
using Toggl.Joey.UI.Fragments;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data;
using XPlatUtils;
using Toggl.Phoebe;

namespace Toggl.Joey.UI.Activities
{
    [Activity (
        Exported = false,
        WindowSoftInputMode = SoftInput.StateHidden)]
    public class EditTimeEntryActivity : BaseActivity
    {
        public static readonly string ExtraTimeEntryId = "com.toggl.timer.time_entry_id";

        private FrameLayout DoneFrameLayout { get; set; }

        private Subscription<ModelChangedMessage> subscriptionModelChanged;
        private TimeEntryModel model;

        protected override void OnCreateActivity (Bundle state)
        {
            base.OnCreateActivity (state);

            var actionBarView = CreateDoneActionBarView ();
            DoneFrameLayout = actionBarView.FindViewById<FrameLayout> (Resource.Id.DoneFrameLayout);
            DoneFrameLayout.Click += OnDoneFrameLayoutClick;

            ActionBar.SetDisplayOptions (
                ActionBarDisplayOptions.ShowCustom,
                (ActionBarDisplayOptions)((int)ActionBarDisplayOptions.ShowCustom
                | (int)ActionBarDisplayOptions.ShowHome
                | (int)ActionBarDisplayOptions.ShowTitle));
            ActionBar.CustomView = actionBarView;

            SetContentView (Resource.Layout.EditTimeEntryActivity);

            if (state == null) {
                model = GetModelFromIntent ();
                if (model == null) {
                    Finish ();
                } else {
                    FragmentManager.BeginTransaction ()
                        .Add (Resource.Id.FrameLayout, new EditTimeEntryFragment (model))
                        .Commit ();
                }
            }
        }

        protected override void OnResumeActivity ()
        {
            base.OnResumeActivity ();

            EnsureModel ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionModelChanged = bus.Subscribe<ModelChangedMessage> (OnModelChanged);
        }

        protected override void OnPause ()
        {
            if (subscriptionModelChanged != null) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                bus.Unsubscribe (subscriptionModelChanged);
                subscriptionModelChanged = null;
            }

            base.OnPause ();
        }

        private TimeEntryModel GetModelFromIntent ()
        {
            var extras = Intent.Extras;
            if (extras == null)
                return null;

            var extraIdStr = extras.GetString (ExtraTimeEntryId);
            Guid extraId;
            if (!Guid.TryParse (extraIdStr, out extraId))
                return null;

            return Model.ById<TimeEntryModel> (extraId);
        }

        private View CreateDoneActionBarView ()
        {
            var inflater = (LayoutInflater)ActionBar.ThemedContext.GetSystemService (LayoutInflaterService);
            return inflater.Inflate (Resource.Layout.DoneActionBarView, null);
        }

        private void OnModelChanged (ModelChangedMessage msg)
        {
            if (Handle == IntPtr.Zero)
                return;

            if (msg.Model == model) {
                EnsureModel ();
            }
        }

        private void OnDoneFrameLayoutClick (object sender, EventArgs e)
        {
            Finish ();
        }

        private void EnsureModel ()
        {
            if (model == null || model.DeletedAt.HasValue) {
                Finish ();
            }
        }
    }
}
