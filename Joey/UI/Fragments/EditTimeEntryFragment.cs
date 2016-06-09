﻿using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Activities;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class EditTimeEntryFragment : Fragment,
        ChangeTimeEntryDurationDialogFragment.IChangeDuration,
        ChangeDateTimeDialogFragment.IChangeDateTime,
        IOnTagSelectedHandler
    {
        private static readonly string TimeEntryIdArgument = "com.toggl.timer.time_entry_id";

        // to avoid weak references to be removed
        private Binding<string, string> durationBinding, projectBinding, clientBinding, descriptionBinding;
        private Binding<DateTime, string> startTimeBinding, stopTimeBinding;
        private Binding<List<string>, List<string>> tagBinding;
        private Binding<bool, ViewStates> isPremiumBinding;
        private Binding<bool, bool> isBillableBinding, billableBinding, isRunningBinding, saveMenuBinding, syncErrorBinding;

        public EditTimeEntryVM ViewModel { get; private set; }
        public TextView DurationTextView { get; private set; }
        public EditText StartTimeEditText { get; private set; }
        public EditText StopTimeEditText { get; private set; }
        public CheckBox BillableCheckBox { get; private set; }
        public TogglField ProjectField { get; private set; }
        public TogglField DescriptionField { get; private set; }
        public TogglTagsField TagsField { get; private set; }
        public IMenuItem SaveMenuItem { get; private set; }

        private View editTimeEntryContent;
        private View editTimeEntryProgressBar;
        private TextView stopTimeEditLabel;
        private ActionBar toolbar;

        private Guid TimeEntryId
        {
            get
            {
                var id = Guid.Empty;
                if (Arguments != null)
                {
                    Guid.TryParse(Arguments.GetString(TimeEntryIdArgument), out id);
                }
                return id;
            }
        }

        public EditTimeEntryFragment()
        {
        }

        public EditTimeEntryFragment(IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base(jref, xfer)
        {
        }

        public static EditTimeEntryFragment NewInstance(string timeEntryId)
        {
            var fragment = new EditTimeEntryFragment();

            var bundle = new Bundle();
            bundle.PutString(TimeEntryIdArgument, timeEntryId);
            fragment.Arguments = bundle;

            return fragment;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.EditTimeEntryFragment, container, false);
            var activityToolbar = view.FindViewById<Toolbar> (Resource.Id.EditTimeEntryFragmentToolbar);
            var activity = (Activity)Activity;

            activity.SetSupportActionBar(activityToolbar);
            toolbar = activity.SupportActionBar;
            toolbar.SetDisplayHomeAsUpEnabled(true);


            var durationLayout = inflater.Inflate(Resource.Layout.DurationTextView, null);
            DurationTextView = durationLayout.FindViewById<TextView> (Resource.Id.DurationTextViewTextView);

            toolbar.SetCustomView(durationLayout, new ActionBar.LayoutParams((int)GravityFlags.Center));
            toolbar.SetDisplayShowCustomEnabled(true);
            toolbar.SetDisplayShowTitleEnabled(false);

            StartTimeEditText = view.FindViewById<EditText> (Resource.Id.StartTimeEditText).SetFont(Font.Roboto);
            StopTimeEditText = view.FindViewById<EditText> (Resource.Id.StopTimeEditText).SetFont(Font.Roboto);
            stopTimeEditLabel = view.FindViewById<TextView> (Resource.Id.StopTimeEditLabel);

            DescriptionField = view.FindViewById<TogglField> (Resource.Id.Description)
                               .DestroyAssistView().DestroyArrow()
                               .SetName(Resource.String.EditTimeEntryFragmentDescription)
                               .SetHint(Resource.String.EditTimeEntryFragmentDescriptionHint);

            ProjectField = view.FindViewById<TogglField> (Resource.Id.Project)
                           .SetName(Resource.String.EditTimeEntryFragmentProject)
                           .SetHint(Resource.String.EditTimeEntryFragmentProjectHint)
                           .SimulateButton();

            TagsField = view.FindViewById<TogglTagsField> (Resource.Id.TagsBit);
            BillableCheckBox = view.FindViewById<CheckBox> (Resource.Id.BillableCheckBox).SetFont(Font.RobotoLight);
            editTimeEntryProgressBar = view.FindViewById<View> (Resource.Id.EditTimeEntryProgressBar);
            editTimeEntryContent = view.FindViewById<View> (Resource.Id.EditTimeEntryContent);

            HasOptionsMenu = true;
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            ViewModel = new EditTimeEntryVM(StoreManager.Singleton.AppState, TimeEntryId);

            // TODO: in theory, this event could be binded but
            // the event "CheckedChange" isn't found when
            // the app is compiled for release. Investigate!
            BillableCheckBox.CheckedChange += (sender, e) =>
            {
                ViewModel.ChangeBillable(BillableCheckBox.Checked);
            };

            DescriptionField.TextField.TextChanged += (sender, e) =>
            {
                ViewModel.ChangeDescription(DescriptionField.TextField.Text);
            };

            DurationTextView.Click += (sender, e) =>
            {
                // TODO: Don't edit duration if Time entry is running?
                if (ViewModel.IsRunning)
                {
                    return;
                }
                ChangeTimeEntryDurationDialogFragment.NewInstance(ViewModel.StopDate, ViewModel.StartDate)
                .SetChangeDurationHandler(this)
                .Show(FragmentManager, "duration_dialog");
            };

            StartTimeEditText.Click += (sender, e) =>
            {
                var title = GetString(Resource.String.ChangeTimeEntryStartTimeDialogTitle);
                ChangeDateTimeDialogFragment.NewInstance(ViewModel.StartDate, title)
                .SetOnChangeTimeHandler(this)
                .Show(FragmentManager, "start_time_dialog");
            };

            StopTimeEditText.Click += (sender, e) =>
            {
                var title = GetString(Resource.String.ChangeTimeEntryStopTimeDialogTitle);
                ChangeDateTimeDialogFragment.NewInstance(ViewModel.StopDate, title)
                .SetOnChangeTimeHandler(this)
                .Show(FragmentManager, "stop_time_dialog");
            };

            ProjectField.TextField.Click += (sender, e) => OpenProjectListActivity();
            ProjectField.Click += (sender, e) => OpenProjectListActivity();
            TagsField.OnPressTagField += OnTagsEditTextClick;

            durationBinding = this.SetBinding(() => ViewModel.Duration, () => DurationTextView.Text);
            startTimeBinding = this.SetBinding(() => ViewModel.StartDate, () => StartTimeEditText.Text)
                               .ConvertSourceToTarget(dateTime => dateTime.ToDeviceTimeString());
            stopTimeBinding = this.SetBinding(() => ViewModel.StopDate, () => StopTimeEditText.Text)
                              .ConvertSourceToTarget(dateTime => dateTime.ToDeviceTimeString());
            projectBinding = this.SetBinding(() => ViewModel.ProjectName, () => ProjectField.TextField.Text);
            clientBinding = this.SetBinding(() => ViewModel.ClientName, () => ProjectField.AssistViewTitle);
            tagBinding = this.SetBinding(() => ViewModel.Tags).WhenSourceChanges(() =>
            {
                TagsField.TagNames = ViewModel.Tags;
            }
                                                                                );
            descriptionBinding = this.SetBinding(() => ViewModel.Description, () => DescriptionField.TextField.Text);
            isPremiumBinding = this.SetBinding(() => ViewModel.IsPremium, () => BillableCheckBox.Visibility)
                               .ConvertSourceToTarget(isVisible => isVisible ? ViewStates.Visible : ViewStates.Gone);
            isRunningBinding = this.SetBinding(() => ViewModel.IsRunning).WhenSourceChanges(() =>
            {
                StopTimeEditText.Visibility = ViewModel.IsRunning ? ViewStates.Gone : ViewStates.Visible;
                stopTimeEditLabel.Visibility = ViewModel.IsRunning ? ViewStates.Gone : ViewStates.Visible;
            });
            isBillableBinding = this.SetBinding(() => ViewModel.IsBillable, () => BillableCheckBox.Checked);
            billableBinding = this.SetBinding(() => ViewModel.IsBillable)
                              .WhenSourceChanges(() =>
            {
                var label = ViewModel.IsBillable ? GetString(Resource.String.CurrentTimeEntryEditBillableChecked) : GetString(Resource.String.CurrentTimeEntryEditBillableUnchecked);
                BillableCheckBox.Text = label;
            });

            // Configure option menu.
            ConfigureOptionMenu();

            // Does project list need to be opened?
            if (Activity.Intent.GetBooleanExtra(EditTimeEntryActivity.StartedByFab, false))
            {
                // Remove the argument so this code is not triggered when EditTimeEntryActivity
                // is launched again after returning from ProjectListActivity
                Activity.Intent.RemoveExtra(EditTimeEntryActivity.StartedByFab);
                OpenProjectListActivity();
            }

            // Finally set content visible.
            editTimeEntryContent.Visibility = ViewStates.Visible;
            editTimeEntryProgressBar.Visibility = ViewStates.Gone;
        }

        public override void OnDestroyView()
        {
            ViewModel?.Dispose();
            base.OnDestroyView();
        }

        public override void OnPause()
        {
            // Save Time entry state every time the fragment is paused.
            ViewModel?.Save();
            base.OnPause();
        }

        private void OpenProjectListActivity()
        {
            var intent = new Intent(Activity, typeof(ProjectListActivity));
            intent.PutExtra(BaseActivity.IntentWorkspaceIdArgument, ViewModel.WorkspaceId.ToString());
            StartActivityForResult(intent, 0);
        }

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == (int)Result.Ok)
            {
                var taskId = GetGuidFromIntent(data, BaseActivity.IntentTaskIdArgument);
                var projectId = GetGuidFromIntent(data, BaseActivity.IntentProjectIdArgument);
                ViewModel.ChangeProjectAndTask(projectId, taskId);
            }
        }

        private void OnTagsEditTextClick(object sender, EventArgs e)
        {
            ChooseTimeEntryTagsDialogFragment.NewInstance(ViewModel.WorkspaceId, ViewModel.Tags)
            .SetOnModifyTagListHandler(this)
            .Show(FragmentManager, "tags_dialog");
        }

        public void OnChangeDateTime(TimeSpan timeDiff, string dialogTag)
        {
            if (dialogTag == "start_time_dialog")
            {
                ViewModel.ChangeTimeEntryStart(timeDiff);
            }
            else
            {
                ViewModel.ChangeTimeEntryStop(timeDiff);
            }
        }

        public void OnCreateNewTag(string newTagData)
        {
            var newTagList = ViewModel.Tags.ToList();
            newTagList.Add(newTagData);
            ViewModel.ChangeTagList(newTagList);
        }

        public void OnModifyTagList(List<string> newTagList)
        {
            ViewModel.ChangeTagList(newTagList);
        }

        public void OnChangeDuration(TimeSpan newDuration)
        {
            ViewModel.ChangeTimeEntryDuration(newDuration);
        }

        IMenu menu;
        MenuInflater inflater;

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            this.menu = menu;
            this.inflater = inflater;
            ConfigureOptionMenu();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            bool dismiss = true;
            if (item == SaveMenuItem && ViewModel != null)
            {
                var savedProperly = ViewModel.Save();
                if (savedProperly == false)
                {
                    dismiss = false;
                    XPlatUtils.ServiceContainer.Resolve<GalaSoft.MvvmLight.Views.IDialogService>()
                    .ShowMessage(EditTimeEntryVM.StartTimeError, EditTimeEntryVM.ErrorTitle);
                }
            }

            if (dismiss)
            {
                Activity.OnBackPressed();
            }
            return base.OnOptionsItemSelected(item);
        }

        private Guid GetGuidFromIntent(Intent data, string id)
        {
            Guid result;
            Guid.TryParse(data.GetStringExtra(id), out result);
            return result;
        }

        // Because the viewModel needs time to be created,
        // this method is called from two points
        private void ConfigureOptionMenu()
        {
            if (ViewModel != null && menu != null)
            {
                if (ViewModel.IsManual)
                {
                    inflater.Inflate(Resource.Menu.SaveItemMenu, menu);
                    SaveMenuItem = menu.FindItem(Resource.Id.saveItem);
                }
            }
        }
    }
}
