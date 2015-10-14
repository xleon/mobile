using System;
using Android.Views;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Views;
using PopupArgs = Android.Widget.PopupMenu.MenuItemClickEventArgs;
using Toggl.Phoebe.Data.Models;

namespace Toggl.Joey.UI.Adapters
{
    public class ClientsAdapter : BaseDataViewAdapter<ClientData>
    {
        protected static readonly int ViewTypeCreateClient = ViewTypeContent + 1;
        public const long CreateClientId = -1;

        public Action<object> HandleClientSelection { get; set; }

        public ClientsAdapter (IDataView<ClientData> view) : base (view)
        {
        }

        public override long GetItemId (int position)
        {
            if (!DataView.IsLoading && position == DataView.Count) {
                return CreateClientId;
            }
            return base.GetItemId (position);
        }

        public override int ViewTypeCount
        {
            get {
                return base.ViewTypeCount + 1;
            }
        }

        public override int GetItemViewType (int position)
        {
            if (GetItemId (position) == CreateClientId) {
                return ViewTypeCreateClient;
            }
            return base.GetItemViewType (position);
        }

        public override int Count
        {
            get {
                var count = base.Count;
                if (!DataView.IsLoading) {
                    count += 1;
                }
                return count;
            }
        }

        protected override View GetModelView (int position, View convertView, ViewGroup parent)
        {
            View view = convertView;

            var viewType = GetItemViewType (position);
            if (viewType == ViewTypeCreateClient) {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                               Resource.Layout.ClientListCreateItem, parent, false);
                    view.FindViewById<TextView> (Resource.Id.CreateLabelTextView).SetFont (Font.Roboto);
                }
            } else {
                if (view == null) {
                    view = LayoutInflater.FromContext (parent.Context).Inflate (
                               Resource.Layout.ClientListClientItem, parent, false);
                    view.Tag = new ClientListItemHolder (view);
                }
                var holder = (ClientListItemHolder)view.Tag;
                holder.Bind ((ClientModel)GetEntry (position));
            }

            return view;
        }

        #region View holders

        public class ClientListItemHolder : ModelViewHolder<ClientModel>
        {
            public TextView ClientTextView { get; private set; }

            public ClientListItemHolder (View root) : base (root)
            {
                ClientTextView = root.FindViewById<TextView> (Resource.Id.ClientTextView).SetFont (Font.Roboto);
            }

            protected override void ResetTrackedObservables ()
            {
                Tracker.MarkAllStale ();

                if (DataSource != null) {
                    Tracker.Add (DataSource, HandleClientPropertyChanged);
                }

                Tracker.ClearStale ();
            }

            protected override void Rebind ()
            {
                // Protect against Java side being GCed
                if (Handle == IntPtr.Zero) {
                    return;
                }

                ResetTrackedObservables ();

                if (DataSource == null) {
                    return;
                }

                ClientTextView.Text = DataSource.Name;
            }

            private void HandleClientPropertyChanged (string prop)
            {
                if (prop == ClientModel.PropertyName) {
                    Rebind ();
                }
            }
        }
        #endregion
    }
}