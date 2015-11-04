using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public interface IOnClientSelectedListener
    {
        void OnClientSelected (ClientData data);
    }

    public class ClientListDialogFragment : BaseDialogFragment
    {
        private const string WorkspaceIdArgument = "workspace_id";
        private ListView listView;
        private ClientListViewModel viewModel;
        private ClientsAdapter adapter;
        private IOnClientSelectedListener listener;

        private Guid WorkspaceId
        {
            get {
                var id = Guid.Empty;
                if (Arguments != null) {
                    Guid.TryParse (Arguments.GetString (WorkspaceIdArgument), out id);
                }
                return id;
            }
        }

        public ClientListDialogFragment ()
        {
        }

        public ClientListDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public static ClientListDialogFragment NewInstance (Guid workspaceId)
        {
            var fragment = new ClientListDialogFragment ();

            var args = new Bundle();
            args.PutString (WorkspaceIdArgument, workspaceId.ToString ());
            fragment.Arguments = args;

            return fragment;
        }

        public async override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            viewModel = new ClientListViewModel (WorkspaceId);
            await viewModel.Init ();
        }

        public override void OnDestroy ()
        {
            viewModel.Dispose ();
            viewModel = null;
            base.OnDestroy ();
        }

        public ClientListDialogFragment SetClientSelectListener (IOnClientSelectedListener listener)
        {
            this.listener = listener;
            return this;
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
                CreateClientDialogFragment.NewInstance (WorkspaceId)
                .SetOnClientSelectedListener (listener)
                .Show (FragmentManager, "new_client_dialog");
            } else {
                listener.OnClientSelected (adapter.GetEntry (e.Position));
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
