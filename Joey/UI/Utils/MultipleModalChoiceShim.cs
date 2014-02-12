using System;
using Android.Views;
using Android.Widget;
using ActionMode = Android.Support.V7.View.ActionMode;
using Activity = Android.Support.V7.App.ActionBarActivity;

namespace Toggl.Joey.UI.Utils
{
    /// <summary>
    /// Multiple modal choice shim provides ListView MultipleModal ChoiceMode implementation for API level 10.
    /// It falls back to default ListView implementation when possible.
    /// </summary>
    public abstract class MultipleModalChoiceShim : Java.Lang.Object
    {
        public static MultipleModalChoiceShim Create (Activity activity, ListView listView, ActionMode.ICallback actionHandler = null)
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Honeycomb) {
                return new MultipleModalChoiceHC (listView, actionHandler);
            } else {
                return new MultipleModalChoiceGB (activity, listView, actionHandler);
            }
        }

        public abstract event EventHandler<AdapterView.ItemClickEventArgs> ItemClick;
        public abstract event EventHandler<ItemCheckedEventArgs> ItemChecked;

        public abstract ActionMode ActionMode { get; }

        public abstract int CheckedItemCount { get; }

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
