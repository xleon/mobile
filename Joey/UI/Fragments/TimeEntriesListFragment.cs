using System;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using XPlatUtils;

namespace Toggl.Joey.UI.Fragments
{
    public class TimeEntriesListFragment : Fragment
    {
        private RecyclerView recyclerView;
        private AllTimeEntriesViewModel viewModel;
        private Subscription<SettingChangedMessage> subscriptionSettingChanged;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.TimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTitleTextView).SetFont (Font.Roboto);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);
            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.LogRecyclerView);
            recyclerView.SetLayoutManager (new LinearLayoutManager (Activity));
            viewModel = new AllTimeEntriesViewModel();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionSettingChanged = bus.Subscribe<SettingChangedMessage> (OnSettingChanged);
        }

        public override void OnResume ()
        {
            EnsureAdapter ();
            base.OnResume ();
        }

        public override bool UserVisibleHint
        {
            get { return base.UserVisibleHint; }
            set {
                base.UserVisibleHint = value;
                EnsureAdapter ();
            }
        }

        #region TimeEntry handlers
        private async void ContinueTimeEntry (TimeEntryModel model)
        {
            DurOnlyNoticeDialogFragment.TryShow (FragmentManager);

            var entry = await model.ContinueAsync ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }

        private async void StopTimeEntry (TimeEntryModel model)
        {
            await model.StopAsync ();

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
        }

        private void ConfirmTimeEntryDeletion (TimeEntryModel model)
        {
        }

        private void OpenTimeEntryEdit (TimeEntryModel model)
        {
            var i = new Intent (Activity, typeof (EditTimeEntryActivity));
            i.PutExtra (EditTimeEntryActivity.ExtraTimeEntryId, model.Id.ToString ());
            StartActivity (i);
        }
        #endregion

        #region TimeEntryGroup handlers
        private async void ContinueTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            DurOnlyNoticeDialogFragment.TryShow (FragmentManager);

            var entry = await entryGroup.Model.ContinueAsync ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStartEvent (TimerStartSource.AppContinue);
        }

        private async void StopTimeEntryGroup (TimeEntryGroup entryGroup)
        {
            await entryGroup.Model.StopAsync ();

            // Ping analytics
            ServiceContainer.Resolve<ITracker> ().SendTimerStopEvent (TimerStopSource.App);
        }

        private void ConfirmTimeEntryGroupDeletion (TimeEntryGroup entryGroup)
        {
        }

        private void OpenTimeEntryGroupEdit (TimeEntryGroup entryGroup)
        {
            var i = new Intent (Activity, typeof (EditTimeEntryActivity));
            string[] guids = entryGroup.TimeEntryGuids;
            i.PutExtra (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids, guids);
            StartActivity (i);
        }
        #endregion

        private void EnsureAdapter ()
        {
            if (recyclerView.GetAdapter() == null) {
                recyclerView.SetAdapter (new TimeEntriesAdapter (viewModel));
                var isGrouped = ServiceContainer.Resolve<SettingsStore> ().GroupedTimeEntries;
            }
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing) {
                var bus = ServiceContainer.Resolve<MessageBus> ();
                if (subscriptionSettingChanged != null) {
                    bus.Unsubscribe (subscriptionSettingChanged);
                    subscriptionSettingChanged = null;
                }
            }
            base.Dispose (disposing);
        }

        private void OnSettingChanged (SettingChangedMessage msg)
        {
            // Protect against Java side being GCed
            if (Handle == IntPtr.Zero) {
                return;
            }

            if (msg.Name == SettingsStore.PropertyGroupedTimeEntries) {
                EnsureAdapter();
            }
        }
    }
}
