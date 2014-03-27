using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using Toggl.Phoebe.Data.Models;
using XPlatUtils;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : ListFragment, AbsListView.IMultiChoiceModeListener
    {
        private ActionMode actionMode;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.LogTimeEntriesListFragment, container, false);
            view.FindViewById<TextView> (Resource.Id.EmptyTitleTextView).SetFont (Font.Roboto);
            view.FindViewById<TextView> (Resource.Id.EmptyTextTextView).SetFont (Font.RobotoLight);
            return view;
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ListView.SetClipToPadding (false);
            ListView.ChoiceMode = (ChoiceMode)AbsListViewChoiceMode.MultipleModal;
            ListView.SetMultiChoiceModeListener (this);
        }

        public override void OnResume ()
        {
            EnsureAdapter ();
            base.OnResume ();
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            var adapter = ListView.Adapter as LogTimeEntriesAdapter;
            if (adapter == null)
                return;

            var model = adapter.GetModel (position);
            if (model == null)
                return;

            adapter.ExpandedPosition = adapter.ExpandedPosition != position ? position : (int?)null;
        }

        private void ContinueTimeEntry (TimeEntryModel model)
        {
            var entry = model.Continue ();

            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));

            DurOnlyNoticeDialogFragment.TryShow (FragmentManager);
        }

        public override bool UserVisibleHint {
            get { return base.UserVisibleHint; }
            set {
                base.UserVisibleHint = value;
                EnsureAdapter ();
            }
        }

        private void EnsureAdapter ()
        {
            if (ListAdapter == null && UserVisibleHint) {
                var adapter = new LogTimeEntriesAdapter ();
                adapter.HandleTimeEntryDeletion = ConfirmTimeEntryDeletion;
                adapter.HandleTimeEntryEditing = OpenTimeEntryEdit;
                adapter.HandleTimeEntryContinue = ContinueTimeEntry;
                ListAdapter = adapter;
            }
        }

        private void ConfirmTimeEntryDeletion (TimeEntryModel model)
        {
            // Make sure that the time entry being edited is persisted (so the changes would actually sync back)
            model.IsPersisted = true;

            var dia = new DeleteTimeEntriesPromptDialogFragment (new List<TimeEntryModel> () { model });
            dia.Show (FragmentManager, "confirm_delete");
        }

        private void OpenTimeEntryEdit (TimeEntryModel model)
        {
            // Make sure that the time entry being edited is persisted (so the changes would actually sync back)
            model.IsPersisted = true;

            var i = new Intent (Activity, typeof(EditTimeEntryActivity));
            i.PutExtra (EditTimeEntryActivity.ExtraTimeEntryId, model.Id.ToString ());
            StartActivity (i);
        }

        void AbsListView.IMultiChoiceModeListener.OnItemCheckedStateChanged (ActionMode mode, int position, long id, bool @checked)
        {
            var checkedCount = ListView.CheckedItemCount;
            mode.Title = String.Format ("{0} selected", checkedCount);
            actionMode = mode;
//            mode.Menu.FindItem (Resource.Id.EditMenuItem).SetEnabled (checkedCount == 1);
        }

        bool ActionMode.ICallback.OnCreateActionMode (ActionMode mode, IMenu menu)
        {
            mode.MenuInflater.Inflate (Resource.Menu.LogTimeEntriesContextMenu, menu);
            return true;
        }

        bool ActionMode.ICallback.OnPrepareActionMode (ActionMode mode, IMenu menu)
        {
            return false;
        }

        bool ActionMode.ICallback.OnActionItemClicked (ActionMode mode, IMenuItem item)
        {
            switch (item.ItemId) {
            case Resource.Id.DeleteMenuItem:
                DeleteCheckedTimeEntries ();
                mode.Finish ();
                return true;
//            case Resource.Id.EditMenuItem:
            // TODO: Show time entry editing
//                return true;
            default:
                return false;
            }
        }

        void ActionMode.ICallback.OnDestroyActionMode (ActionMode mode)
        {
            actionMode = null;
        }

        private void DeleteCheckedTimeEntries ()
        {
            var adapter = ListView.Adapter as LogTimeEntriesAdapter;
            if (adapter == null)
                return;

            // Find models to delete:
            var checkedPositions = ListView.CheckedItemPositions;
            var arrSize = checkedPositions.Size ();
            var toDelete = new List<TimeEntryModel> (arrSize);

            for (var i = 0; i < arrSize; i++) {
                var position = checkedPositions.KeyAt (i);
                var isChecked = checkedPositions.Get (position);
                if (!isChecked)
                    continue;

                var model = adapter.GetModel (position);
                if (model != null)
                    toDelete.Add (model);
            }

            // Delete models:
            var dia = new DeleteTimeEntriesPromptDialogFragment (toDelete);
            dia.Show (FragmentManager, "confirm_delete");
        }

        public void CloseActionMode ()
        {
            if (actionMode != null) {
                actionMode.Finish ();
            }
        }

        public override void OnStop ()
        {
            base.OnStop ();
            CloseActionMode ();
        }
    }
}
