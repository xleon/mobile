using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Content;

namespace Toggl.Joey.UI.Activities
{
    [Activity (Exported = false)]
    public class StartTimeEntryActivity : BaseActivity
    {
        protected override void OnCreate (Bundle state)
        {
            base.OnCreate (state);

            Title = Resources.GetString (Resource.String.ChooseProjectNewTimeEntryTitle);

            ActionBar.SetDisplayHomeAsUpEnabled (true);

            SetContentView (Resource.Layout.StartTimeEntryActivity);
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
