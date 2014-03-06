using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.Models;
using Toggl.Joey.UI.Adapters;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class LogTimeEntriesListFragment : ListFragment, AbsListView.IMultiChoiceModeListener
    {
        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);

            ListView.SetClipToPadding (false);
            ListView.ChoiceMode = (ChoiceMode)AbsListViewChoiceMode.MultipleModal;
            ListView.SetMultiChoiceModeListener (this);
            ListAdapter = new LogTimeEntriesAdapter ();
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            var adapter = ListView.Adapter as LogTimeEntriesAdapter;
            if (adapter == null)
                return;

            var model = adapter.GetModel (position);
            if (model == null)
                return;

            // TODO Call Edit screen for this row
        }

        void AbsListView.IMultiChoiceModeListener.OnItemCheckedStateChanged (ActionMode mode, int position, long id, bool @checked)
        {
            var checkedCount = ListView.CheckedItemCount;
            mode.Title = String.Format ("{0} selected", checkedCount);
            mode.Menu.FindItem (Resource.Id.EditMenuItem).SetEnabled (checkedCount == 1);
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
