using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Views;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;

using Android.Support.V7.App;
using Android.Support.V7.Widget;

namespace Toggl.Joey.UI.Adapters
{
    public class ProjectsRecyclerAdapter : RecyclerView.Adapter
    {
        private readonly ProjectsClientDataView dataView;

        static int TYPE_PROJECTS = 0;
        static int TYPE_CLIENT_SECTION = 1;


        List<DataHolder> datas = new List<DataHolder> ();

        public ProjectsRecyclerAdapter () : this(new ProjectsClientDataView())
        {
            
        }

        public class DataHolder
        {
            public ProjectModel Project { get; private set; }
            public bool ClientHeader { get; private set; }
            public string ClientName { get; private set; }

            public int ViewType { get { return ClientHeader ? TYPE_CLIENT_SECTION : TYPE_PROJECTS; } }

            public DataHolder(ProjectModel project, bool clientHandler = false)
            {
                Project = project;
                ClientHeader = clientHandler;

                var client = Project.Client;
                ClientName = client == null ? "No client" : client.Name;
            }
        }

        private ProjectsRecyclerAdapter(ProjectsClientDataView dataView)
        {
            this.dataView = dataView;
            this.dataView.Updated += OnDataViewUpdated;
            dataView.Reload ();
        }

        bool pass = true;

        void OnDataViewUpdated (object sender, EventArgs e)
        {
            if (Handle == IntPtr.Zero) {
                return;
            }

            if (!pass) {
                return;
            }

            pass = false;

            var guid = Guid.Empty;
            bool emptyClientEntered = false;

            foreach (ProjectData proj in dataView.Data) {
                if (proj == null) {
                    continue;
                }

                var model = (ProjectModel)proj;

                if (model.Client == null && !emptyClientEntered) {
                    datas.Add (new DataHolder (model, true));
                    emptyClientEntered = true;
                } else if (model.Client != null && (guid == Guid.Empty || model.Client.Id != guid)) {
                    datas.Add (new DataHolder (model, true));
                    guid = model.Client.Id;
                }
                datas.Add (new DataHolder (model));
            }

            NotifyDataSetChanged ();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            var v = LayoutInflater.From (parent.Context).Inflate (viewType == TYPE_PROJECTS ?  Resource.Layout.ProjectFragmenItem : Resource.Layout.ProjectFragmentClientItem, parent, false);
            return viewType == TYPE_PROJECTS ? (RecyclerView.ViewHolder)new ItemViewHolder (v) : (RecyclerView.ViewHolder)new ClientItemViewHolder(v);
        }

        public override int GetItemViewType (int position)
        {
            return datas [position].ViewType;
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            var dataHolderEntity = datas [position];
            if (holder is ItemViewHolder) {
                (holder as ItemViewHolder).BindFromDataHolder (dataHolderEntity);
            } else if (holder is ClientItemViewHolder) {
                (holder as ClientItemViewHolder).BindFromDataHolder (dataHolderEntity);
            }
        }

        public override int ItemCount {
            get {
                return datas.Count;
            }
        }

        public class ClientItemViewHolder : RecyclerView.ViewHolder
        {
            public TextView Text { get; private set; }

            public ClientItemViewHolder(View v) : base(v)
            {
                Text = v.FindViewById<TextView>(Resource.Id.ProjectFragmentClientItemTextView);
            }

            public void BindFromDataHolder(DataHolder holder)
            {
                Text.Text = holder.ClientName;
            }
        }

        public class ItemViewHolder : RecyclerView.ViewHolder
        {
            public View Color { get; private set; }
            public TextView Text { get; private set; }

            public ItemViewHolder(View v) : base(v)
            {
                Color = v.FindViewById(Resource.Id.ProjectFragmentItemColorView);
                Text = v.FindViewById<TextView>(Resource.Id.ProjectFragmentItemTimePeriodTextView);
            }

            public void BindFromDataHolder(DataHolder holder)
            {
                var proj = holder.Project;
               // var color = Color.ParseColor (proj.GetHexColor ());
                //Color.SetBackgroundColor (color);
                Text.Text = proj.Name;
            }

        }
    }

    class ProjectsClientDataView : IDataView<object>, IDisposable
    {
        private ProjectAndTaskView dataView;

        public ProjectsClientDataView ()
        {
            dataView = new ProjectAndTaskView ();
            dataView.Updated += OnProjectTaskViewUpdated;
        }

        public void Dispose ()
        {
            if (dataView != null) {
                dataView.Dispose ();
                dataView.Updated -= OnProjectTaskViewUpdated;
                dataView = null;
            }
        }

        void OnProjectTaskViewUpdated (object sender, EventArgs e)
        {
            OnUpdated ();
        }

        public event EventHandler Updated;

        private void OnUpdated() 
        {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public void Reload()
        {
            if (dataView != null) {
                dataView.Reload ();
            }
        }

        public void LoadMore ()
        {
            if (dataView != null) {
                dataView.LoadMore ();
            }
        }

        public IEnumerable<object> Data
        {
            get { 
                if (!dataView.Workspaces.Any ()) {
                    return Enumerable.Empty<object>();
                }

                var workspace = dataView.Workspaces.ElementAt (0);
                var projects = workspace.Projects;

                List<ProjectData> dd = new List<ProjectData> ();

                foreach (var data in projects) {
                    dd.Add (data.Data);
                }
                return dd;
            }
        }

        public long Count 
        {
            get { return Data.Count(); }
        }

        public bool HasMore
        {
            get {
                if (dataView != null) {
                    return dataView.HasMore;
                }
                return false;
            }
        }

        public bool IsLoading
        {
            get {
                if (dataView != null) {
                    return dataView.IsLoading;
                }
                return false;
            }
        }
    }
}

