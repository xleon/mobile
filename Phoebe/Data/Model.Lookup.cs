using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public partial class Model
    {
        private static Dictionary<Type, MemoryModelCache> modelCaches =
            new Dictionary<Type, MemoryModelCache> ();

        /// <summary>
        /// Returns all of the cached shared models.
        /// </summary>
        /// <returns>Enumerable for cached model instances.</returns>
        /// <typeparam name="T">Type of model to get cached instances for.</typeparam>
        public static IEnumerable<T> GetCached<T> ()
            where T : Model
        {
            MemoryModelCache cache;
            if (!modelCaches.TryGetValue (typeof(T), out cache))
                return Enumerable.Empty<T> ();

            return cache.All<T> ();
        }

        public static IEnumerable<Model> GetCached (Type type)
        {
            MemoryModelCache cache;
            if (!modelCaches.TryGetValue (type, out cache))
                return Enumerable.Empty<Model> ();

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
        public static T Update<T> (T model)
            where T : Model
        {
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

        /// <summary>
        /// Retrieves the specified model either from cache or from model store.
        /// </summary>
        /// <returns>The shared instance, null if not found.</returns>
        /// <param name="id">Id for the model.</param>
        /// <typeparam name="T">Type of the model.</typeparam>
        public static T Get<T> (Guid id)
            where T : Model
        {
            return (T)Get (typeof(T), id);
        }

        private static Model Get (Type type, Guid id, bool autoLoad = true)
        {
            Model inst = null;
            MemoryModelCache cache;

            // Look through in-memory models:
            if (modelCaches.TryGetValue (type, out cache)) {
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
        public static T GetByRemoteId<T> (long remoteId)
            where T : Model
        {
            return (T)GetByRemoteId (typeof(T), remoteId);
        }

        internal static Model GetByRemoteId (Type type, long remoteId)
        {
            Model inst = null;
            MemoryModelCache cache;

            // Look through in-memory models:
            if (modelCaches.TryGetValue (type, out cache)) {
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

        public static IModelQuery<T> Query<T> (Expression<Func<T, bool>> predicate = null)
            where T : Model, new()
        {
            var modelStore = ServiceContainer.Resolve<IModelStore> ();
            return modelStore.Query (predicate, (e) => e.Select (UpdateQueryModel));
        }

        private static T UpdateQueryModel<T> (T model)
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

        private static void MakeShared (Model model)
        {
            if (model.Id == null)
                model.Id = Guid.NewGuid ();

            MemoryModelCache cache;
            var type = model.GetType ();

            if (!modelCaches.TryGetValue (type, out cache)) {
                modelCaches [type] = cache = new MemoryModelCache ();
            }

            cache.Add (model);
            model.IsShared = true;
        }
    }
}