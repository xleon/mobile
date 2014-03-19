using System;
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
    }
}
