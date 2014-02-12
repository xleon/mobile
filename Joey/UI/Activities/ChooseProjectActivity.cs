using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Toggl.Joey.UI.Fragments;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Exported = false)]
    public class ChooseProjectActivity : BaseActivity
    {
        public static readonly string TimeEntryIdExtra = "com.toggl.android.time_entry_id";

        protected override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            Title = Resources.GetString (Resource.String.ChooseProjectTitle);

            ActionBar.SetDisplayHomeAsUpEnabled (true);

            SetContentView (Resource.Layout.ChooseProjectActivity);

            if (state == null) {
                var projectListFragment = new ChooseTimeEntryProjectListFragment (TimeEntryId);

                FragmentManager.BeginTransaction ()
                    .Add (Resource.Id.ChooseTimeEntryProjectListFragment, projectListFragment)
                    .Commit ();
            }
        }

        private Guid TimeEntryId {
            get {
                try {
                    return Guid.Parse (Intent.Extras.GetString (TimeEntryIdExtra));
                } catch (NullReferenceException) {
                    return Guid.Empty;
                } catch (FormatException) {
                    return Guid.Empty;
                }
            }
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                var intent = new Intent (this, typeof(TimeEntriesActivity));
                intent.AddFlags (ActivityFlags.ClearTop);
                Finish ();
                return true;
            }

            return base.OnOptionsItemSelected (item);
        }
    }
}
