using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.ViewModels;
using ActionBar = Android.Support.V7.App.ActionBar;
using Activity = Android.Support.V7.App.AppCompatActivity;
using Fragment = Android.Support.V4.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Joey.UI.Fragments
{
    public interface IOnClientSelectedHandler
    {
        void OnClientSelected (ClientData data);
    }

    public class ClientListDialogFragment : BaseDialogFragment
    {
        private const string WorkspaceIdArgument = "workspace_id";
        private ListView listView;
        private ClientListViewModel viewModel;
        private IOnClientSelectedHandler clientSelectedHandler;

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
            viewModel = await ClientListViewModel.Init (WorkspaceId);
        }

        public override void OnDestroy ()
        {
            viewModel.Dispose ();
            viewModel = null;
            base.OnDestroy ();
        }

        public ClientListDialogFragment SetClientSelectListener (IOnClientSelectedHandler handler)
        {
            clientSelectedHandler = handler;
            return this;
        }

        public override Dialog OnCreateDialog (Bundle savedInstanceState)
        {
            // Mvvm ligth utility to generate an adapter from
            // an Observable collection.
            var clientsAdapter = viewModel.ClientDataCollection.GetAdapter (GetClientView);

            var dia = new AlertDialog.Builder (Activity)
            .SetTitle (Resource.String.SelectClientTitle)
            .SetAdapter (clientsAdapter, (IDialogInterfaceOnClickListener)null)
            .SetPositiveButton (Resource.String.ClientsNewClient, OnCreateButtonClicked)
            .Create ();

            listView = dia.ListView;
            listView.Clickable = true;
            listView.ItemClick += OnItemClick;

            return dia;
        }

        private View GetClientView (int position, ClientData clientData, View convertView)
        {
            View view = convertView ?? LayoutInflater.FromContext (Activity).Inflate (Resource.Layout.TagListItem, null);
            var nameCheckedTextView = view.FindViewById<CheckedTextView> (Resource.Id.NameCheckedTextView).SetFont (Font.Roboto);
            nameCheckedTextView.Text = clientData.Name;
            return view;
        }

        private void OnItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            clientSelectedHandler.OnClientSelected (viewModel.ClientDataCollection [e.Position]);
            Dismiss ();
        }

        private void OnCreateButtonClicked (object sender, DialogClickEventArgs args)
        {
            CreateClientDialogFragment.NewInstance (WorkspaceId)
            .SetOnClientSelectedListener (clientSelectedHandler)
            .Show (FragmentManager, "new_client_dialog");
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
