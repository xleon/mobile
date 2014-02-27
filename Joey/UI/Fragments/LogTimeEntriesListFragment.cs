using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using ActionMode = Android.Support.V7.View.ActionMode;
using Activity = Android.Support.V7.App.ActionBarActivity;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : ListFragment, ActionMode.ICallback
    {
        private MultipleModalChoiceShim shim;

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ListAdapter = new LogTimeEntriesAdapter ();
            shim = MultipleModalChoiceShim.Create (Activity as Activity, ListView, this);
            shim.ItemClick += OnItemClick;
            shim.ItemChecked += OnItemChecked;
        }

        private void OnItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            var adapter = ListView.Adapter as LogTimeEntriesAdapter;
            if (adapter == null)
                return;

            var model = adapter.GetModel (e.Position);
            if (model == null)
                return;

            // TODO Call Edit screen for this row
        }

        private void OnItemChecked (object sender, MultipleModalChoiceShim.ItemCheckedEventArgs e)
        {
            e.ActionMode.Title = String.Format ("{0} selected", shim.CheckedItemCount);
            e.ActionMode.Menu.FindItem (Resource.Id.EditMenuItem).SetEnabled (shim.CheckedItemCount == 1);
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
            case Resource.Id.EditMenuItem:
                // TODO: Show time entry editing
                return true;
            default:
                return false;
            }
        }

        void ActionMode.ICallback.OnDestroyActionMode (ActionMode mode)
        {
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
            dia.Show (FragmentManager, "dialog");
        }
    }
}
