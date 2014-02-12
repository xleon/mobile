using System;
using Android.Views;
using Android.Widget;
using ActionMode = Android.Support.V7.View.ActionMode;
using Activity = Android.Support.V7.App.ActionBarActivity;

namespace Toggl.Joey.UI.Utils
{
    /// <summary>
    /// Multiple modal choice shim provides ListView MultipleModal ChoiceMode implementation for API level 10.
    /// </summary>
    public class MultipleModalChoiceShim : Java.Lang.Object, ActionMode.ICallback
    {
        private readonly Activity activity;
        private readonly ListView listView;
        private readonly ActionMode.ICallback nestedCallback;
        private ActionMode actionMode;
        private int checkedItemCount;

        public MultipleModalChoiceShim (Activity activity, ListView listView, ActionMode.ICallback actionHandler = null)
        {
            this.activity = activity;
            this.listView = listView;
            this.nestedCallback = actionHandler;

            listView.ChoiceMode = ChoiceMode.Multiple;
            listView.ItemClick += OnItemClick;
            listView.ItemLongClick += OnItemLongClick;
        }

        public event EventHandler<AdapterView.ItemClickEventArgs> ItemClick;
        public event EventHandler<ItemCheckedEventArgs> ItemChecked;

        public ActionMode ActionMode {
            get { return actionMode; }
        }

        public int CheckedItemCount {
            get { return checkedItemCount; }
        }

        private void OnItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            var isChecked = listView.CheckedItemPositions.Get (e.Position, false);

            if (actionMode != null) {
                checkedItemCount += isChecked ? 1 : -1;

                if (checkedItemCount == 0) {
                    actionMode.Finish ();
                } else {
                    OnItemCheckedStateChanged (actionMode);
                }
            } else {
                // Revert selection
                listView.SetItemChecked (e.Position, !isChecked);

                if (ItemClick != null) {
                    ItemClick (sender, e);
                }
            }
        }

        private void OnItemLongClick (object sender, AdapterView.ItemLongClickEventArgs e)
        {
            if (actionMode == null) {
                activity.StartSupportActionMode (this);

                if (actionMode != null) {
                    listView.SetItemChecked (e.Position, true);
                    listView.PerformHapticFeedback (FeedbackConstants.LongPress);
                    checkedItemCount = 1;

                    OnItemCheckedStateChanged (actionMode);
                }
            }
            e.Handled = true;
        }

        bool ActionMode.ICallback.OnCreateActionMode (ActionMode mode, IMenu menu)
        {
            var success = true;
            if (nestedCallback != null) {
                success = nestedCallback.OnCreateActionMode (mode, menu);
            }

            if (success) {
                actionMode = mode;
                listView.LongClickable = false;
            }

            return success;
        }

        bool ActionMode.ICallback.OnPrepareActionMode (ActionMode mode, IMenu menu)
        {
            if (nestedCallback != null) {
                return nestedCallback.OnPrepareActionMode (mode, menu);
            }
            return false;
        }

        bool ActionMode.ICallback.OnActionItemClicked (ActionMode mode, IMenuItem item)
        {
            if (nestedCallback != null) {
                return nestedCallback.OnActionItemClicked (mode, item);
            }
            return false;
        }

        void ActionMode.ICallback.OnDestroyActionMode (ActionMode mode)
        {
            actionMode = null;
            listView.ClearChoices ();
            listView.InvalidateViews ();
            listView.LongClickable = true;
            checkedItemCount = 0;

            if (nestedCallback != null) {
                nestedCallback.OnDestroyActionMode (mode);
            }
        }

        private void OnItemCheckedStateChanged (ActionMode mode)
        {
            if (ItemChecked != null) {
                ItemChecked (listView, new ItemCheckedEventArgs (actionMode));
            }
        }

        [Serializable]
        public sealed class ItemCheckedEventArgs : EventArgs
        {
            public ItemCheckedEventArgs (ActionMode mode)
            {
                ActionMode = mode;
            }

            public ActionMode ActionMode { get; private set; }
        }
    }
}
