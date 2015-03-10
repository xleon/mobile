
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Joey.UI.Decorations;
using Toggl.Joey.UI.Views;

using Android.Support.V4.App;
using Android.Support.V7.Widget;

using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;

namespace Toggl.Joey.UI.Fragments
{
    public class GroupedEditTimeEntryFragment : Fragment
    {
        private TimeEntryGroup entryGroup;

        public RecyclerView recyclerView;
        public RecyclerView.Adapter adapter;
        public RecyclerView.LayoutManager layoutManager;


        public GroupedEditTimeEntryFragment(TimeEntryGroup entryGroup) {
            this.entryGroup = entryGroup;
        }

        protected TextView DurationTextView { get; private set; }

        protected EditTimeEntryBit ProjectBit { get; private set; }

        protected EditTimeEntryBit TaskBit { get; private set; }

        protected EditTimeEntryBit DescriptionBit { get; private set; }

        protected EditTimeEntryTagsBit TagsBit { get; private set; }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.GroupedEditTimeEntryFragment, container, false);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.recyclerView);

            layoutManager = new LinearLayoutManager (Activity);
            recyclerView.SetLayoutManager (layoutManager);

            adapter = new GroupTimeEntriesAdapter(entryGroup); 
            recyclerView.SetAdapter (adapter);

            var decoration = new ItemDividerDecoration (Activity.ApplicationContext);
            recyclerView.AddItemDecoration (decoration);

            DurationTextView = view.FindViewById<TextView> (Resource.Id.GroupedEditTimeEntryFragmentDurationTextView);

            ProjectBit = view.FindViewById<EditTimeEntryBit> (Resource.Id.GroupedEditTimeEntryFragmentProject).SetName (Resource.String.BaseEditTimeEntryFragmentProject).SimulateButton();
            TaskBit = view.FindViewById<EditTimeEntryBit> (Resource.Id.GroupedEditTimeEntryFragmentTask).DestroyAssistView ().SetName (Resource.String.BaseEditTimeEntryFragmentTask).SimulateButton();
            DescriptionBit = view.FindViewById<EditTimeEntryBit> (Resource.Id.GroupedEditTimeEntryFragmentDescription).DestroyAssistView().DestroyArrow().SetName (Resource.String.BaseEditTimeEntryFragmentDescription);
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

        protected virtual void Rebind() 
        {
            // Reset tracked Observables

            if (entryGroup == null) {
                return;
            }

            DurationTextView.Text = entryGroup.Duration.ToString ();


        }

        public override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            // Create your fragment here
        }
    }



}

