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

        const int TYPE_PROJECTS = 0;
        const int TYPE_CLIENT_SECTION = 1;

        public IEnumerable<object> CachedData;

        public ProjectsRecyclerAdapter () : this (new ProjectsClientDataView())
        {

        }

        private ProjectsRecyclerAdapter (ProjectsClientDataView dataView)
        {
            this.dataView = dataView;
            this.dataView.Updated += OnDataViewUpdated;
            dataView.Reload ();
        }

        void OnDataViewUpdated (object sender, EventArgs e)
        {
            CachedData = dataView.Data.ToList();
            NotifyDataSetChanged ();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType)
        {
            var v = LayoutInflater.From (parent.Context).Inflate (viewType == TYPE_PROJECTS ?  Resource.Layout.ProjectFragmenItem : Resource.Layout.ProjectFragmentClientItem, parent, false);
            return viewType == TYPE_PROJECTS ? (RecyclerView.ViewHolder)new ItemViewHolder (v) : (RecyclerView.ViewHolder)new ClientItemViewHolder (v);
        }

        public override int GetItemViewType (int position)
        {
            var dataholder = CachedData.ElementAt (position) as ProjectsClientDataView.DataHolder;
            return dataholder.ClientHeader ? TYPE_CLIENT_SECTION : TYPE_PROJECTS;;
        }

        public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position)
        {
            var dataholder = CachedData.ElementAt (position) as ProjectsClientDataView.DataHolder;
            if (holder is ItemViewHolder) {
                (holder as ItemViewHolder).BindFromDataHolder (dataholder);
            } else if (holder is ClientItemViewHolder) {
                (holder as ClientItemViewHolder).BindFromDataHolder (dataholder);
            }
        }

        public override int ItemCount
        {
            get {
                return CachedData == null ? 0 : CachedData.Count();
            }
        }

        public class ClientItemViewHolder : RecyclerView.ViewHolder
        {
            public TextView Text { get; private set; }

            public ClientItemViewHolder (View v) : base (v)
            {
                Text = v.FindViewById<TextView> (Resource.Id.ProjectFragmentClientItemTextView);
            }

            public void BindFromDataHolder (ProjectsClientDataView.DataHolder holder)
            {
                Text.Text = holder.Project.Client == null ? "No project" : holder.Project.Client.Name;
            }
        }

        public class ItemViewHolder : RecyclerView.ViewHolder
        {
            public View Color { get; private set; }
            public TextView Text { get; private set; }

            public ItemViewHolder (View v) : base (v)
            {
                Color = v.FindViewById (Resource.Id.ProjectFragmentItemColorView);
                Text = v.FindViewById<TextView> (Resource.Id.ProjectFragmentItemTimePeriodTextView);
            }

            public void BindFromDataHolder (ProjectsClientDataView.DataHolder holder)
            {
                var proj = holder.Project;
                var color = Android.Graphics.Color.ParseColor (proj.GetHexColor ());
                Color.SetBackgroundColor (color);
                Text.Text = proj.Name;
            }

        }
    }

    public class ProjectsClientDataView : IDataView<object>, IDisposable
    {
        private ProjectAndTaskView dataView;

        public ProjectsClientDataView ()
        {
            dataView = new ProjectAndTaskView (sortByClients: true);
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
                    yield break;
                }

                var workspace = dataView.Workspaces.ElementAt (0);
                var projects = workspace.Projects;

                var emptyClientEntered = false;
                var guid = Guid.Empty;

                foreach (var proj in projects.Select (x => x.Data).ToList ()) {
                    if (proj == null) {
                        continue;
                    }

                    var model = (ProjectModel)proj;
                    var addClientHeader = true;

                    if (model.Client == null && !emptyClientEntered) {
                        emptyClientEntered = true;
                    } else if (model.Client != null && (guid == Guid.Empty || model.Client.Id != guid)) {
                        model.Client.LoadAsync ();
                        guid = model.Client.Id;
                    } else {
                        addClientHeader = false;
                    }

                    if (addClientHeader) {
                        yield return new DataHolder (model, true);
                    }

                    yield return new DataHolder (model);
                }
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

        public class DataHolder
        {
            public ProjectModel Project { get; private set; }
            public bool ClientHeader { get; private set; }

            public DataHolder (ProjectModel project, bool clientHandler = false)
            {
                Project = project;
                ClientHeader = clientHandler;
            }
        }

    }
}

