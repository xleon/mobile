using System;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Decorations;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Utils;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.ActionBarActivity;
using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;

namespace Toggl.Joey.UI.Fragments
{
    public class GroupedEditTimeEntryFragment : Fragment
    {
        private readonly TimeEntryGroup entryGroup;
        private RecyclerView recyclerView;
        private RecyclerView.Adapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        public GroupedEditTimeEntryFragment (TimeEntryGroup entryGroup)
        {
            this.entryGroup = entryGroup;
        }

        protected TextView DurationTextView { get; private set; }

        protected TogglField ProjectBit { get; private set; }

        protected TogglField TaskBit { get; private set; }

        protected TogglField DescriptionBit { get; private set; }

        protected EditTimeEntryTagsBit TagsBit { get; private set; }

        protected ActionBar Toolbar { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.GroupedEditTimeEntryFragment, container, false);

            var toolbar = view.FindViewById<Toolbar> (Resource.Id.GroupedEditTimeEntryFragmentToolbar);

            var activity = (Activity)Activity;
            activity.SetSupportActionBar (toolbar);
            Toolbar = activity.SupportActionBar;
            Toolbar.SetDisplayHomeAsUpEnabled (true);

            var durationLayout = inflater.Inflate (Resource.Layout.DurationTextView, null);

            DurationTextView = durationLayout.FindViewById<TextView> (Resource.Id.DurationTextViewTextView);

            Toolbar.SetCustomView (durationLayout, new ActionBar.LayoutParams (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
            Toolbar.SetDisplayShowCustomEnabled (true);
            Toolbar.SetDisplayShowTitleEnabled (false);

            HasOptionsMenu = true;

            adapter = new GroupedEditAdapter (entryGroup);
            layoutManager = new LinearLayoutManager (Activity);
            var decoration = new ItemDividerDecoration (Activity.ApplicationContext);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.recyclerView);
            recyclerView.SetLayoutManager (layoutManager);
            recyclerView.SetAdapter (adapter);
            recyclerView.AddItemDecoration (decoration);

            ProjectBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentProject).SetName (Resource.String.BaseEditTimeEntryFragmentProject).SimulateButton();
            TaskBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentTask).DestroyAssistView ().SetName (Resource.String.BaseEditTimeEntryFragmentTask).SimulateButton();
            DescriptionBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentDescription).DestroyAssistView().DestroyArrow().SetName (Resource.String.BaseEditTimeEntryFragmentDescription);
            TagsBit = view.FindViewById<EditTimeEntryTagsBit> (Resource.Id.GroupedEditTimeEntryFragmentTags);

            TagsBit.FullClick += OnTagsEditTextClick;

            Rebind ();

            return view;
        }

        private void OnTagsEditTextClick (object sender, EventArgs e)
        {
            if (entryGroup == null) {
                return;
            }

            new ChooseTimeEntryTagsDialogFragment (entryGroup.Model).Show (FragmentManager, "tags_dialog");
        }

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            menu.Add (Resource.String.BaseEditTimeEntryFragmentSaveButtonText).SetShowAsAction (ShowAsAction.Always);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                Activity.OnBackPressed ();
            }
            return base.OnOptionsItemSelected (item);
        }

        protected virtual void Rebind()
        {
            // Reset tracked Observables

            if (entryGroup == null) {
                return;
            }

            DurationTextView.Text = entryGroup.GetFormattedDuration ();
        }


    }
}

