using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public class ClientListFragment : BaseDialogFragment
    {
        private ListView listView;
        private ClientListViewModel viewModel;
        private Guid workspaceId;
        private ProjectModel model;
        private ClientsAdapter adapter;

        public ClientListFragment ()
        {
        }

        public ClientListFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public ClientListFragment (Guid workspaceId, ProjectModel project)
        {
            this.workspaceId = workspaceId;
            this.model = project;
            viewModel = new ClientListViewModel (this.workspaceId, model);
        }

        public async override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            if (viewModel == null) {
                viewModel = new ClientListViewModel (workspaceId, model);
            }
            await viewModel.Init ();

            if (viewModel.Model.Workspace == null || viewModel.Model.Workspace.Id == Guid.Empty) {
                Dismiss ();
            }
        }

        private void OnModelLoaded (object sender, EventArgs e)
        {
            if (!viewModel.IsLoading) {
                if (viewModel.Model != null) {
                    viewModel.ClientListDataView.Updated += OnWorkspaceClientsUpdated;
                } else {
                    Dismiss ();
                }
            }
        }

        private void OnWorkspaceClientsUpdated (object sender, EventArgs args)
        {
            if (!viewModel.ClientListDataView.IsLoading) {
            }
        }

        public override void OnDestroy ()
        {
            if (viewModel != null) {
                viewModel.OnIsLoadingChanged -= OnModelLoaded;
                viewModel.ClientListDataView.Updated -= OnWorkspaceClientsUpdated;
                viewModel.Dispose ();
                viewModel = null;
            }

            base.OnDestroy ();
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            adapter = new ClientsAdapter (viewModel.ClientListDataView);
            var dia = new AlertDialog.Builder (Activity)
            .SetTitle (Resource.String.SelectClientTitle)
            .SetAdapter (new ClientsAdapter (viewModel.ClientListDataView), (IDialogInterfaceOnClickListener)null)
            .SetPositiveButton (Resource.String.ChooseTimeEntryTagsDialogOk, delegate {})
            .Create ();

            listView = dia.ListView;
            listView.Clickable = true;
            listView.ItemClick += OnItemClick;

            return dia;
        }

        private void OnItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            if (e.Id == ClientsAdapter.CreateClientId) {
                new CreateClientDialogFragment (model).Show (FragmentManager, "new_client_dialog");
            } else {
                viewModel.SaveClient ((ClientModel) adapter.GetEntry (e.Position));
            }
            Dismiss ();
        }

        public override void OnActivityResult (int requestCode, int resultCode, Intent data)
        {
            base.OnActivityResult (requestCode, resultCode, data);
            if (requestCode == NewProjectFragment.ClientSelectedRequestCode) {
                if (resultCode == (int)Result.Ok) {
                    Activity.Finish();
                }
            }
        }
    }
}
