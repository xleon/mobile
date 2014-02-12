using System;
using Android.Views;
using Android.Widget;
using ActionMode = Android.Support.V7.View.ActionMode;

namespace Toggl.Joey.UI.Utils
{
    public class MultipleModalChoiceHC : MultipleModalChoiceShim, AbsListView.IMultiChoiceModeListener
    {
        private readonly ListView listView;
        private readonly ActionMode.ICallback actionHandler;
        private WrappedActionMode actionMode;

        public MultipleModalChoiceHC (ListView listView, ActionMode.ICallback actionHandler)
        {
            this.listView = listView;
            this.actionHandler = actionHandler;

            listView.ChoiceMode = (ChoiceMode)AbsListViewChoiceMode.MultipleModal;
            listView.ItemClick += OnItemClick;
            listView.SetMultiChoiceModeListener (this);
        }

        public override ActionMode ActionMode {
            get { return actionMode; }
        }

        public override int CheckedItemCount {
            get { return listView.CheckedItemCount; }
        }

        public override event EventHandler<AdapterView.ItemClickEventArgs> ItemClick;
        public override event EventHandler<ItemCheckedEventArgs> ItemChecked;

        private void OnItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            if (ItemClick != null) {
                ItemClick (sender, e);
            }
        }

        void AbsListView.IMultiChoiceModeListener.OnItemCheckedStateChanged (Android.Views.ActionMode mode, int position, long id, bool @checked)
        {
            if (ItemChecked != null) {
                ItemChecked (listView, new ItemCheckedEventArgs (actionMode));
            }
        }

        bool Android.Views.ActionMode.ICallback.OnCreateActionMode (Android.Views.ActionMode mode, IMenu menu)
        {
            if (actionMode == null || actionMode.Wrapped != mode) {
                actionMode = new WrappedActionMode (mode);
            }

            if (actionHandler != null) {
                return actionHandler.OnCreateActionMode (actionMode, menu);
            }
            return false;
        }

        bool Android.Views.ActionMode.ICallback.OnPrepareActionMode (Android.Views.ActionMode mode, IMenu menu)
        {
            if (actionMode == null || actionMode.Wrapped != mode) {
                actionMode = new WrappedActionMode (mode);
            }

            if (actionHandler != null) {
                return actionHandler.OnPrepareActionMode (actionMode, menu);
            }
            return false;
        }

        bool Android.Views.ActionMode.ICallback.OnActionItemClicked (Android.Views.ActionMode mode, IMenuItem item)
        {
            if (actionMode == null || actionMode.Wrapped != mode) {
                actionMode = new WrappedActionMode (mode);
            }

            if (actionHandler != null) {
                return actionHandler.OnActionItemClicked (actionMode, item);
            }
            return false;
        }

        void Android.Views.ActionMode.ICallback.OnDestroyActionMode (Android.Views.ActionMode mode)
        {
            if (actionMode == null || actionMode.Wrapped != mode) {
                actionMode = new WrappedActionMode (mode);
            }

            if (actionHandler != null) {
                actionHandler.OnDestroyActionMode (actionMode);
            }
        }

        private class WrappedActionMode : ActionMode
        {
            private readonly Android.Views.ActionMode actionMode;

            public WrappedActionMode (Android.Views.ActionMode actionMode)
            {
                this.actionMode = actionMode;
            }

            public Android.Views.ActionMode Wrapped {
                get { return actionMode; }
            }

            public override void Finish ()
            {
                Wrapped.Finish ();
            }

            public override void Invalidate ()
            {
                Wrapped.Invalidate ();
            }

            public override void SetSubtitle (int resId)
            {
                Wrapped.SetSubtitle (resId);
            }

            public override void SetTitle (int resId)
            {
                Wrapped.SetSubtitle (resId);
            }

            public override View CustomView {
                get { return Wrapped.CustomView; }
                set { Wrapped.CustomView = value; }
            }

            public override IMenu Menu {
                get { return Wrapped.Menu; }
            }

            public override MenuInflater MenuInflater {
                get { return Wrapped.MenuInflater; }
            }

            public override Java.Lang.ICharSequence SubtitleFormatted {
                get { return Wrapped.SubtitleFormatted; }
                set { Wrapped.SubtitleFormatted = value; }
            }

            public override Java.Lang.ICharSequence TitleFormatted {
                get { return Wrapped.TitleFormatted; }
                set { Wrapped.TitleFormatted = value; }
            }
        }
    }
}

