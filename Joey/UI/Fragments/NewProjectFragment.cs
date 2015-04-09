using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Android.Text;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Joey.UI.Views;
using XPlatUtils;

using Toolbar = Android.Support.V7.Widget.Toolbar;
using Fragment = Android.Support.V4.App.Fragment;
using ActionBarActivity = Android.Support.V7.App.ActionBarActivity;
using ActionBar = Android.Support.V7.App.ActionBar;

namespace Toggl.Joey.UI.Fragments
{
    public class NewProjectFragment : Fragment
    {
        protected ActionBar Toolbar { get; private set; }

        private readonly ProjectModel model;

        private bool isSaving;

        public TogglField ProjectBit { get; private set; }
        public ColorPickerRecyclerView ColorPicker { get; private set; }

        public NewProjectFragment (WorkspaceModel workspace)
        {
            model = new ProjectModel {
                Workspace = workspace,
                IsActive = true,
                IsPrivate = true
            };
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate (Resource.Layout.NewProjectFragment, container, false);

            var activity = (ActionBarActivity)this.Activity;

            var toolbar = view.FindViewById<Toolbar> (Resource.Id.NewProjectsFragmentToolbar);
            activity.SetSupportActionBar (toolbar);
            Toolbar = activity.SupportActionBar;
            Toolbar.SetDisplayHomeAsUpEnabled (true);
            Toolbar.SetTitle (Resource.String.NewProjectTitle);

            ProjectBit = view.FindViewById<TogglField> (Resource.Id.NewProjectProjectNameBit).DestroyAssistView().DestroyArrow().SetName (Resource.String.NewProjectProjectFieldName);
            ProjectBit.TextField.TextChanged += ProjectBitTextChangedHandler;

            ColorPicker = view.FindViewById<ColorPickerRecyclerView> (Resource.Id.NewProjectColorPickerRecyclerView);
            ColorPicker.SelectedColorChanged += (object sender, int e) => {
                model.Color = e;
            };

            HasOptionsMenu = true;

            return view;
        }

        public override void OnStart ()
        {
            base.OnStart ();
            var inputService = (InputMethodManager)Activity.GetSystemService (Context.InputMethodService);
            ProjectBit.TextField.PostDelayed (delegate {
                inputService.ShowSoftInput (ProjectBit.TextField, ShowFlags.Implicit);
            }, 100);
        }

        private async void SaveButtonHandler (object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace (model.Name)) {
                new AlertDialog.Builder (Activity)
                .SetTitle (Resource.String.NewProjectEmptyDialogTitle)
                .SetMessage (Resource.String.NewProjectEmptyDialogMessage)
                .SetPositiveButton (Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                .Show();
                return;
            }

            if (isSaving) {
                return;
            }

            isSaving = true;

            try {
                var dataStore = ServiceContainer.Resolve<IDataStore> ();
                Guid clientId = (model.Client == null) ? Guid.Empty : model.Client.Id;
                var existWithName = await dataStore.Table<ProjectData>().ExistWithNameAsync (model.Name, clientId);

                if (existWithName) {
                    new AlertDialog.Builder (Activity)
                    .SetTitle (Resource.String.NewProjectDuplicateDialogTitle)
                    .SetMessage (Resource.String.NewProjectDuplicateDialogMessage)
                    .SetPositiveButton (Resource.String.NewProjectEmptyDialogPositiveButtonTitle, (EventHandler<DialogClickEventArgs>)null)
                    .Show();
                } else {
                    await model.SaveAsync();
                    Activity.OnBackPressed();
                }
            } finally {
                isSaving = false;
            }
        }

        private void ProjectBitTextChangedHandler (object sender, TextChangedEventArgs e)
        {
            var t = ProjectBit.TextField.Text;
            if (t != model.Name) {
                model.Name = t;
            }
        }

        public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
        {
            menu.Add (Resource.String.NewProjectSaveButtonText).SetShowAsAction (ShowAsAction.Always);
        }

        public override bool OnOptionsItemSelected (IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home) {
                Activity.OnBackPressed ();
            } else {
                SaveButtonHandler (this, null);
            }
            return base.OnOptionsItemSelected (item);
        }
    }
}

