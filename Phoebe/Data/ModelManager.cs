using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Toggl.Phoebe.Net;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class ModelManager
    {
        private readonly Dictionary<Type, MemoryModelCache> caches = new Dictionary<Type, MemoryModelCache> ();
        #pragma warning disable 0414
        private readonly Subscription<AuthChangedMessage> subscriptionAuthChanged;
        #pragma warning restore 0414

        public ModelManager ()
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            subscriptionAuthChanged = bus.Subscribe<AuthChangedMessage> (OnAuthChanged);
        }

        /// <summary>
        /// Returns all of the cached shared models.
        /// </summary>
        /// <returns>Enumerable for cached model instances.</returns>
        /// <typeparam name="T">Type of model to get cached instances for.</typeparam>
        public IEnumerable<T> Cached<T> ()
            where T : Model
        {
            MemoryModelCache cache;

            lock (caches) {
                if (!caches.TryGetValue (typeof(T), out cache))
                    return Enumerable.Empty<T> ();
            }

            return cache.All<T> ();
        }

        public IEnumerable<Model> Cached (Type type)
        {
            MemoryModelCache cache;

            lock (caches) {
                if (!caches.TryGetValue (type, out cache))
                    return Enumerable.Empty<Model> ();
            }

            return cache.All<Model> ();
        }

        /// <summary>
        /// Gets the shared instance for this model (by Id or RemoteId). If no existing model (in memory and model
        /// store) is found, the given model is promoted to a shared instance status.
        /// When an existing model is found, the data from given instance is merged into the shared instance
        /// automatically.
        /// </summary>
        /// <returns>The shared shared model instance.</returns>
        /// <param name="model">Model for which a shared instance should be returned.</param>
        /// <typeparam name="T">Type of model.</typeparam>
        public T Update<T> (T model)
            where T : Model
        {
            lock (Model.SyncRoot) {
                if (model.IsShared)
                    return model;

                T sharedModel = null;

                // First, try to look-up the shared model based on the Id
                if (model.Id.HasValue)
                    sharedModel = (T)Get (model.GetType (), model.Id.Value);
                if (model.RemoteId != null) {
                    if (sharedModel == null) {
                        // If lookup by id failed, try remote id
                        sharedModel = (T)GetByRemoteId (model.GetType (), model.RemoteId.Value);
                    } else {
                        // Enforce integrity, no duplicate RemoteId's
                        if (GetByRemoteId (model.GetType (), model.RemoteId.Value) != null) {
                            throw new IntegrityException ("RemoteId is not unique, cannot make shared.");
                        }
                    }
                }

                if (sharedModel != null) {
                    sharedModel.Merge (model);
                } else {
                    MakeShared (model);
                    sharedModel = model;
                }

                return sharedModel;
            }
        }

        /// <summary>
        /// Retrieves the specified model either from cache or from model store.
        /// </summary>
        /// <returns>The shared instance, null if not found.</returns>
        /// <param name="id">Id for the model.</param>
        /// <typeparam name="T">Type of the model.</typeparam>
        public T Get<T> (Guid id)
            where T : Model
        {
            lock (Model.SyncRoot) {
                return (T)Get (typeof(T), id);
            }
        }

        public Model Get (Type type, Guid id)
        {
            lock (Model.SyncRoot) {
                return Get (type, id, true);
            }
        }

        private Model Get (Type type, Guid id, bool autoLoad)
        {
            Model inst = null;
            MemoryModelCache cache;

            // Look through in-memory models:
            lock (caches) {
                caches.TryGetValue (type, out cache);
            }
            if (cache != null) {
                inst = cache.GetById<Model> (id);
            }

            // Try to load from database:
            if (inst == null && autoLoad) {
                var modelStore = ServiceContainer.Resolve<IModelStore> ();
                inst = modelStore.Get (type, id);
                if (inst != null) {
                    MakeShared (inst);
                    return inst;
                }
            }

            return inst;
        }

        /// <summary>
        /// Retrieves a shared model by unique RemoteId from cache or from model store.
        /// </summary>
        /// <returns>The shared instance, null if not found.</returns>
        /// <param name="remoteId">Remote identifier.</param>
        /// <typeparam name="T">Type of the model.</typeparam>
        public T GetByRemoteId<T> (long remoteId)
            where T : Model
        {
            lock (Model.SyncRoot) {
                return (T)GetByRemoteId (typeof(T), remoteId);
            }
        }

        internal Model GetByRemoteId (Type type, long remoteId)
        {
            Model inst = null;
            MemoryModelCache cache;

            lock (Model.SyncRoot) {
                // Look through in-memory models:
                lock (caches) {
                    caches.TryGetValue (type, out cache);
                }
                if (cache != null) {
                    inst = cache.GetByRemoteId<Model> (remoteId);
                }

                // Try to load from database:
                if (inst == null) {
                    var modelStore = ServiceContainer.Resolve<IModelStore> ();
                    inst = modelStore.GetByRemoteId (type, remoteId);
                    // Check that this model isn't in memory already and having been modified
                    if (inst != null && cache != null && cache.GetById<Model> (inst.Id.Value) != null) {
                        inst = null;
                    }
                    // Mark the loaded model as shared
                    if (inst != null) {
                        MakeShared (inst);
                    }
                }

                return inst;
            }
        }

        public IModelQuery<T> Query<T> (Expression<Func<T, bool>> predicate = null)
            where T : Model, new()
        {
            lock (Model.SyncRoot) {
                var modelStore = ServiceContainer.Resolve<IModelStore> ();
                return modelStore.Query (predicate, (e) => e.Select (UpdateQueryModel));
            }
        }

        private T UpdateQueryModel<T> (T model)
            where T : Model, new()
        {
            var cached = (T)Get (typeof(T), model.Id.Value, false);
            if (cached != null) {
                cached.Merge (model);
                return cached;
            }

            MakeShared (model);
            return model;
        }

        private void MakeShared (Model model)
        {
            if (model.Id == null)
                model.Id = Guid.NewGuid ();

            MemoryModelCache cache;
            var type = model.GetType ();

            lock (caches) {
                if (!caches.TryGetValue (type, out cache)) {
                    caches [type] = cache = new MemoryModelCache ();
                }
            }

            cache.Add (model);
            model.IsShared = true;
        }

        internal void NotifyRemoteIdChanged (Model model, long? oldValue, long? newValue)
        {
            if (!model.IsShared)
                return;

            lock (Model.SyncRoot) {
                // Update cache index
                MemoryModelCache cache;
                lock (caches) {
                    caches.TryGetValue (model.GetType (), out cache);
                }
                if (cache != null) {
                    cache.UpdateRemoteId (model, oldValue, newValue);
                }
            }
        }

        private void OnAuthChanged (AuthChangedMessage msg)
        {
            if (msg.AuthManager.IsAuthenticated)
                return;

            lock (Model.SyncRoot) {
                // Wipe caches on logout
                caches.Clear ();
            }
        }
    }
}
