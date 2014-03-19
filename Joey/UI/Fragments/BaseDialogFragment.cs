using System;
using Android.Support.V4.App;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace Toggl.Joey.UI.Fragments
{
    public abstract class BaseDialogFragment : DialogFragment
    {
        protected BaseDialogFragment ()
        {
        }

        protected BaseDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public override void Show (FragmentManager manager, string tag)
        {
            // Make sure we don't show the dialog twice
            var frag = manager.FindFragmentByTag (tag);
            if (frag != null)
                return;

            base.Show (manager, tag);
        }
    }
}
