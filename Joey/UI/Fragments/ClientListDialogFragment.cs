using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Praeclarum.Bind;
using Toggl.Joey.UI.Adapters;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
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

    public class ClientListDialogFragment : BaseDialogFragment, IOnClientSelectedListener
    {
        private ListView listView;
        private ClientListViewModel viewModel;
        private ClientsAdapter adapter;
        private Guid workspaceId;
        private Binding binding;
        private IOnClientSelectedListener listener;

        public ClientListDialogFragment ()
        {
        }

        public ClientListDialogFragment (IntPtr jref, Android.Runtime.JniHandleOwnership xfer) : base (jref, xfer)
        {
        }

        public ClientListDialogFragment (Guid workspaceId, IOnClientSelectedListener listener)
        {
            this.workspaceId = workspaceId;
            this.listener = listener;
        }

        public async override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);

            viewModel = new ClientListViewModel (workspaceId);
            await viewModel.Init ();
        }

        public override void OnDestroy ()
        {
            viewModel.Dispose ();
            viewModel = null;
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

            return dia;
        }

        private void OnItemClick (object sender, AdapterView.ItemClickEventArgs e)
        {
            if (e.Id == ClientsAdapter.CreateClientId) {
                CreateClientDialogFragment.NewInstance (workspaceId)
                .SetOnClientSelectedListener (this)
                .Show (FragmentManager, "new_client_dialog");
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

        #region IOnClientSelectedListener implementation

        public void OnClientSelected (ClientData data)
        {
            throw new NotImplementedException ();
        }

        #endregion
    }
}
