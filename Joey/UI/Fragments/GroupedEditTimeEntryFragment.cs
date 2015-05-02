using System;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Android.Content;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Decorations;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.DataObjects;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using MeasureSpec = Android.Views.View.MeasureSpec;

namespace Toggl.Joey.UI.Fragments
{
    public class GroupedEditTimeEntryFragment : Fragment
    {
        private readonly Handler handler = new Handler ();
        private PropertyChangeTracker propertyTracker;
        private readonly TimeEntryGroup entryGroup;
        private RecyclerView recyclerView;
        private RecyclerView.Adapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private bool canRebind;
        private bool descriptionChanging;
        private bool autoCommitScheduled;


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
            (adapter as GroupedEditAdapter).ItemClick += HandleTimeEntryClick;
            layoutManager = new LinearLayoutManager (Activity);
            var decoration = new ItemDividerDecoration (Activity.ApplicationContext);

            recyclerView = view.FindViewById<RecyclerView> (Resource.Id.recyclerView);
            recyclerView.SetLayoutManager (layoutManager);
            recyclerView.SetAdapter (adapter);
            recyclerView.AddItemDecoration (decoration);

            ProjectBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentProject).SetName (Resource.String.BaseEditTimeEntryFragmentProject).SimulateButton();
            ProjectBit.Click += OnProjectEditTextClick;
            ProjectBit.TextField.Click += OnProjectEditTextClick;

            TaskBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentTask).DestroyAssistView ().SetName (Resource.String.BaseEditTimeEntryFragmentTask).SimulateButton();
            DescriptionBit = view.FindViewById<TogglField> (Resource.Id.GroupedEditTimeEntryFragmentDescription).DestroyAssistView().DestroyArrow().SetName (Resource.String.BaseEditTimeEntryFragmentDescription);
            DescriptionBit.TextField.TextChanged += OnDescriptionTextChanged;
            DescriptionBit.TextField.EditorAction += OnDescriptionEditorAction;
            DescriptionBit.TextField.FocusChange += OnDescriptionFocusChange;

            TagsBit = view.FindViewById<EditTimeEntryTagsBit> (Resource.Id.GroupedEditTimeEntryFragmentTags);

            TagsBit.FullClick += OnTagsEditTextClick;

            Rebind ();

            return view;
        }

        public override void OnStart ()
        {
            base.OnStart ();

            propertyTracker = new PropertyChangeTracker ();

            canRebind = true;

            Rebind ();
        }

        public override void OnStop ()
        {
            base.OnStop ();
            canRebind = false;

            if (propertyTracker != null) {
                propertyTracker.Dispose ();
                propertyTracker = null;
            }
        }

        void HandleTimeEntryClick (object sender, TimeEntryData timeEntry)
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

                    if (entry.Task != null) {
                        propertyTracker.Add (entry.Task, HandleTaskPropertyChanged);
                    }
                }
            }

            propertyTracker.ClearStale ();
        }

        private void HandleTimeEntryPropertyChanged (string prop)
        {
            if (prop == TimeEntryModel.PropertyProject
                    || prop == TimeEntryModel.PropertyTask
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

        private void HandleTaskPropertyChanged (string prop)
        {
            if (prop == TaskModel.PropertyName) {
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

            DurationTextView.Text = entryGroup.GetFormattedDuration ();

            if (!descriptionChanging && DescriptionBit.TextField.Text != entryGroup.Description) {
                DescriptionBit.TextField.Text = entryGroup.Description;
                DescriptionBit.TextField.SetSelection (DescriptionBit.TextField.Text.Length);
            }

            if (entryGroup.Project != null) {
                ProjectBit.TextField.Text = entryGroup.Project.Name;
                if (entryGroup.Project.Client != null) {
                    ProjectBit.SetAssistViewTitle (entryGroup.Project.Client.Name);
                } else {
                    ProjectBit.DestroyAssistView ();
                }
            }

        }

        private void CommitDescriptionChanges ()
        {
            if (entryGroup != null && descriptionChanging) {
                entryGroup.Description = DescriptionBit.TextField.Text;
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

            descriptionChanging = entryGroup != null && DescriptionBit.TextField.Text != entryGroup.Description;

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

