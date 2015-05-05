using System;
using Android.Content;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Decorations;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Views;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;

namespace Toggl.Joey.UI.Fragments
{
    public class GroupedEditTimeEntryFragment : Fragment
    {
        // logica objects
        private EditTimeEntryGroupView viewModel;
        private readonly Handler handler = new Handler ();
        private bool canRebind;
        private bool descriptionChanging;
        private bool autoCommitScheduled;
        private TimeEntryGroup entryGroup;
        private PropertyChangeTracker propertyTracker;

        // visual objects
        private RecyclerView recyclerView;
        private RecyclerView.Adapter adapter;
        private RecyclerView.LayoutManager layoutManager;
        private TextView durationTextView;
        private TogglField projectBit;
        private TogglField descriptionBit;
        private EditTimeEntryTagsBit tagsBit;
        private ActionBar toolbar;

        public GroupedEditTimeEntryFragment ()
        {
        }

        public GroupedEditTimeEntryFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.GroupedEditTimeEntryFragment, container, false);
            var mToolbar = view.FindViewById<Toolbar> (Resource.Id.GroupedEditTimeEntryFragmentToolbar);
            var activity = (Activity)Activity;

            activity.SetSupportActionBar (mToolbar);
            toolbar = activity.SupportActionBar;
            toolbar.SetDisplayHomeAsUpEnabled (true);

            var durationLayout = inflater.Inflate (Resource.Layout.DurationTextView, null);
            durationTextView = durationLayout.FindViewById<TextView> (Resource.Id.DurationTextViewTextView);

            toolbar.SetCustomView (durationLayout, new ActionBar.LayoutParams (ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
            toolbar.SetDisplayShowCustomEnabled (true);
            toolbar.SetDisplayShowTitleEnabled (false);

            HasOptionsMenu = true;

            layoutManager = new LinearLayoutManager (Activity);
            var decoration = new ItemDividerDecoration (Activity.ApplicationContext);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.recyclerView);
            recyclerView.SetLayoutManager (layoutManager);
            recyclerView.AddItemDecoration (decoration);

            projectBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentProject).SetName (Resource.String.BaseEditTimeEntryFragmentProject).SimulateButton();
            projectBit.Click += OnProjectEditTextClick;
            projectBit.TextField.Click += OnProjectEditTextClick;

            descriptionBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentDescription).DestroyAssistView().DestroyArrow().SetName (Resource.String.BaseEditTimeEntryFragmentDescription);
            descriptionBit.TextField.TextChanged += OnDescriptionTextChanged;
            descriptionBit.TextField.EditorAction += OnDescriptionEditorAction;
            descriptionBit.TextField.FocusChange += OnDescriptionFocusChange;

            tagsBit = view.FindViewById<EditTimeEntryTagsBit> (Resource.Id.GroupedEditTimeEntryFragmentTags);
            tagsBit.FullClick += OnTagsEditTextClick;

            return view;
        }

        public override void OnStart ()
        {
            base.OnStart ();

            var extras = Activity.Intent.Extras;
            if (extras == null) {
                Activity.Finish ();
            }

            var extraGuids = extras.GetStringArray (EditTimeEntryActivity.ExtraGroupedTimeEntriesGuids);
            viewModel = new EditTimeEntryGroupView (extraGuids);
            viewModel.OnIsLoadingChanged += OnModelLoaded;
            viewModel.Init ();
        }

        public override void OnStop ()
        {
            base.OnStop ();
            canRebind = false;

            if (propertyTracker != null) {
                propertyTracker.Dispose ();
                propertyTracker = null;
            }

            if (viewModel != null) {
                viewModel.OnIsLoadingChanged -= OnModelLoaded;
                viewModel.Dispose ();
                viewModel = null;
            }
        }

        private void OnModelLoaded (object sender, EventArgs e)
        {
            if (!viewModel.IsLoading) {
                if (viewModel != null) {
                    entryGroup = viewModel.Model;
                    propertyTracker = new PropertyChangeTracker ();
                    canRebind = true;
                    Rebind ();
                } else {
                    Activity.Finish ();
                }
            }
        }

        private void HandleTimeEntryClick (TimeEntryData timeEntry)
        {
            var intent = new Intent (Activity, typeof (EditTimeEntryActivity));
            intent.PutExtra (EditTimeEntryActivity.ExtraTimeEntryId, timeEntry.Id.ToString());
            StartActivity (intent);
        }

        private void OnTagsEditTextClick (object sender, EventArgs e)
        {
            if (entryGroup == null) {
                return;
            }

            new ChooseTimeEntryTagsDialogFragment (entryGroup.Model).Show (FragmentManager, "tags_dialog");
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            Activity.OnBackPressed ();

            return base.OnOptionsItemSelected (item);
        }


        private void ResetTrackedObservables ()
        {
            return;

            if (propertyTracker == null) {
                return;
            }

            propertyTracker.MarkAllStale ();

            foreach (var data in entryGroup.TimeEntryList) {
                var entry = (TimeEntryModel)data;
                if (entry != null) {
                    propertyTracker.Add (entry, HandleTimeEntryPropertyChanged);

                    if (entry.Project != null) {
                        propertyTracker.Add (entry.Project, HandleProjectPropertyChanged);

                        if (entry.Project.Client != null) {
                            propertyTracker.Add (entry.Project.Client, HandleClientPropertyChanged);
                        }
                    }
                }
            }

            propertyTracker.ClearStale ();
        }

        private void HandleTimeEntryPropertyChanged (string prop)
        {
            if (prop == TimeEntryModel.PropertyProject
                    || prop == TimeEntryModel.PropertyState
                    || prop == TimeEntryModel.PropertyStartTime
                    || prop == TimeEntryModel.PropertyStopTime
                    || prop == TimeEntryModel.PropertyDescription
                    || prop == TimeEntryModel.PropertyIsBillable) {
                Rebind ();
            }
        }

        private void HandleProjectPropertyChanged (string prop)
        {
            if (prop == ProjectModel.PropertyClient
                    || prop == ProjectModel.PropertyName
                    || prop == ProjectModel.PropertyColor) {
                Rebind ();
            }
        }

        private void HandleClientPropertyChanged (string prop)
        {
            if (prop == ClientModel.PropertyName) {
                Rebind ();
            }
        }

        protected virtual void Rebind()
        {
            ResetTrackedObservables ();

            if (entryGroup == null) {
                return;
            }

            // Set adapter
            adapter = new GroupedEditAdapter (entryGroup);
            (adapter as GroupedEditAdapter).HandleTapTimeEntry = HandleTimeEntryClick;
            recyclerView.SetAdapter (adapter);

            durationTextView.Text = entryGroup.GetFormattedDuration ();

            if (!descriptionChanging && descriptionBit.TextField.Text != entryGroup.Description) {
                descriptionBit.TextField.Text = entryGroup.Description;
                descriptionBit.TextField.SetSelection (descriptionBit.TextField.Text.Length);
            }

            if (entryGroup.Project != null) {
                projectBit.TextField.Text = entryGroup.Project.Name;
                if (entryGroup.Project.Client != null) {
                    projectBit.SetAssistViewTitle (entryGroup.Project.Client.Name);
                } else {
                    projectBit.DestroyAssistView ();
                }
            }

        }

        private void CommitDescriptionChanges ()
        {
            if (entryGroup != null && descriptionChanging) {
                entryGroup.Description = descriptionBit.TextField.Text;
            }
            descriptionChanging = false;
            CancelDescriptionChangeAutoCommit ();
        }

        private void AutoCommitDescriptionChanges ()
        {
            if (!autoCommitScheduled) {
                return;
            }
            autoCommitScheduled = false;
            CommitDescriptionChanges ();
        }

        private void ScheduleDescriptionChangeAutoCommit ()
        {
            if (autoCommitScheduled) {
                return;
            }

            autoCommitScheduled = true;
            handler.PostDelayed (AutoCommitDescriptionChanges, 1000);
        }

        private void CancelDescriptionChangeAutoCommit ()
        {
            if (!autoCommitScheduled) {
                return;
            }

            handler.RemoveCallbacks (AutoCommitDescriptionChanges);
            autoCommitScheduled = false;
        }

        private void OnDescriptionTextChanged (object sender, Android.Text.TextChangedEventArgs e)
        {
            if (!canRebind) {
                return;
            }

            descriptionChanging = entryGroup != null && descriptionBit.TextField.Text != entryGroup.Description;

            CancelDescriptionChangeAutoCommit ();
            if (descriptionChanging) {
                ScheduleDescriptionChangeAutoCommit ();
            }
        }

        private void OnDescriptionFocusChange (object sender, View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus) {
                CommitDescriptionChanges ();
            }
        }

        private void OnDescriptionEditorAction (object sender, TextView.EditorActionEventArgs e)
        {
            if (e.ActionId == Android.Views.InputMethods.ImeAction.Done) {
                CommitDescriptionChanges ();
            }
            e.Handled = false;
        }

        private void OnProjectEditTextClick (object sender, EventArgs e)
        {
            if (entryGroup == null) {
                return;
            }

            var intent = new Intent (Activity, typeof (ProjectListActivity));
            intent.PutExtra (ProjectListActivity.ExtraTimeEntriesIds, entryGroup.TimeEntryGuids);
            StartActivity (intent);
        }

    }
}

