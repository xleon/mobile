using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.NewModels
{
    public abstract class Model<T> : IModel
        where T : CommonData, new()
    {
        private static readonly string Tag = String.Concat ("Model<", typeof(T).Name, ">");

        private static string GetPropertyName<U> (Expression<Func<Model<T>, U>> expr)
        {
            return expr.ToPropertyName ();
        }

        protected static readonly string PropertyId = GetPropertyName (m => m.Id);

        protected static void MarkDirty (T data)
        {
            data.IsDirty = true;
            data.ModifiedAt = Time.UtcNow;
            data.RemoteRejected = false;
        }

        protected static Guid? GetOptionalId (IModel model)
        {
            if (model == null)
                return null;
            if (model.Id == Guid.Empty)
                return null;
            return model.Id;
        }

        private readonly Subscription<DataChangeMessage> subscriptionDataChange;
        private TaskCompletionSource<object> loadingTCS;
        private bool isLoaded;
        private T data;

        protected Model () : this (new T ())
        {
            this.isLoaded = true;
        }

        protected Model (Guid id) : this (new T () { Id = id })
        {
            this.isLoaded = id == Guid.Empty;
        }

        protected Model (T data)
        {
            this.data = data;
            this.isLoaded = true;

            // Listen for global data changes
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionDataChange = bus.Subscribe<DataChangeMessage> (OnDataChange);

            InitializeRelations ();
        }

        private void OnDataChange (DataChangeMessage msg)
        {
            if (data.Matches (msg.Data)) {
                Data = Duplicate ((T)msg.Data);
            }
        }

        protected virtual void InitializeRelations ()
        {
        }

        protected abstract T Duplicate (T data);

        public async Task LoadAsync ()
        {
            if (loadingTCS != null) {
                await loadingTCS.Task;
                return;
            }
            if (isLoaded)
                return;

            loadingTCS = new TaskCompletionSource<object> ();
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var pk = Data.Id;

            var rows = await dataStore.Table<T> ()
                .Where (m => m.Id == pk && m.DeletedAt == null)
                .Take (1)
                .QueryAsync ();

            // When the data is not found in the database, leave the object as a temporary one, hoping that it will
            // be committed to the database soon enough and picked up by the change monitor
            if (rows.Count > 0) {
                Data = rows [0];
            }

            isLoaded = true;
            loadingTCS.SetResult (null);
            loadingTCS = null;
        }

        public async Task SaveAsync ()
        {
            OnBeforeSave ();

            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            Data = await dataStore.PutAsync (Data);
        }

        protected abstract void OnBeforeSave ();

        protected async void EnsureLoaded ()
        {
            if (isLoaded || loadingTCS != null)
                return;

            try {
                await LoadAsync ().ConfigureAwait (false);
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<Logger> ();
                log.Warning (Tag, ex, "Failed to auto-load data.");
            }
        }

        protected void OnPropertyChanged (string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) {
                handler (this, new PropertyChangedEventArgs (propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Guid Id {
            get { return data.Id; }
        }

        protected virtual void DetectChangedProperties (T oldData, T newData)
        {
            if (oldData.Id != newData.Id)
                OnPropertyChanged (PropertyId);
        }

        protected void MutateData (Action<T> mutator)
        {
            var newData = Duplicate (Data);
            mutator (newData);
            MarkDirty (newData);
            Data = newData;
        }

        public T Data {
            get { return data; }
            set {
                var oldData = data ?? new T ();
                var newData = value ?? new T ();

                data = value;

                DetectChangedProperties (oldData, newData);
            }
        }
    }
}
