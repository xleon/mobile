using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class JsonExtensions
    {
        public static CommonData Import (this CommonJson json, IDataStoreContext ctx,
                                         Guid? localIdHint = null, bool forceUpdate = false)
        {
            var type = json.GetType ();
            if (type == typeof(ClientJson)) {
                return Import ((ClientJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(ProjectJson)) {
                return Import ((ProjectJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(ProjectUserJson)) {
                return Import ((ProjectUserJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(TagJson)) {
                return Import ((TagJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(TaskJson)) {
                return Import ((TaskJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(TimeEntryJson)) {
                return Import ((TimeEntryJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(UserJson)) {
                return Import ((UserJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(WorkspaceJson)) {
                return Import ((WorkspaceJson)json, ctx, localIdHint, forceUpdate);
            } else if (type == typeof(WorkspaceUserJson)) {
                return Import ((WorkspaceUserJson)json, ctx, localIdHint, forceUpdate);
            }
            throw new InvalidOperationException (String.Format ("Unknown type of {0}", type));
        }

        public static CommonJson Export (this CommonData data, IDataStoreContext ctx)
        {
            var type = data.GetType ();
            if (type == typeof(ClientData)) {
                return Export ((ClientData)data, ctx);
            } else if (type == typeof(ProjectData)) {
                return Export ((ProjectData)data, ctx);
            } else if (type == typeof(ProjectUserData)) {
                return Export ((ProjectUserData)data, ctx);
            } else if (type == typeof(TagData)) {
                return Export ((TagData)data, ctx);
            } else if (type == typeof(TaskData)) {
                return Export ((TaskData)data, ctx);
            } else if (type == typeof(TimeEntryData)) {
                return Export ((TimeEntryData)data, ctx);
            } else if (type == typeof(UserData)) {
                return Export ((UserData)data, ctx);
            } else if (type == typeof(WorkspaceData)) {
                return Export ((WorkspaceData)data, ctx);
            } else if (type == typeof(WorkspaceUserData)) {
                return Export ((WorkspaceUserData)data, ctx);
            }
            throw new InvalidOperationException (String.Format ("Unknown type of {0}", type));
        }

        public static ClientData Import (this ClientJson json, IDataStoreContext ctx,
                                         Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<ClientJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static ClientJson Export (this ClientData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<ClientJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static ProjectData Import (this ProjectJson json, IDataStoreContext ctx,
                                          Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<ProjectJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static ProjectJson Export (this ProjectData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<ProjectJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static ProjectUserData Import (this ProjectUserJson json, IDataStoreContext ctx,
                                              Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<ProjectUserJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static ProjectUserJson Export (this ProjectUserData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<ProjectUserJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static TagData Import (this TagJson json, IDataStoreContext ctx,
                                      Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<TagJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static TagJson Export (this TagData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<TagJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static TaskData Import (this TaskJson json, IDataStoreContext ctx,
                                       Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<TaskJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static TaskJson Export (this TaskData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<TaskJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static TimeEntryData Import (this TimeEntryJson json, IDataStoreContext ctx,
                                            Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<TimeEntryJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static TimeEntryJson Export (this TimeEntryData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<TimeEntryJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static UserData Import (this UserJson json, IDataStoreContext ctx,
                                       Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<UserJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static UserJson Export (this UserData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<UserJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static WorkspaceData Import (this WorkspaceJson json, IDataStoreContext ctx,
                                            Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<WorkspaceJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static WorkspaceJson Export (this WorkspaceData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<WorkspaceJsonConverter> ();
            return converter.Export (ctx, data);
        }

        public static WorkspaceUserData Import (this WorkspaceUserJson json, IDataStoreContext ctx,
                                                Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<WorkspaceUserJsonConverter> ();
            return converter.Import (ctx, json, localIdHint, forceUpdate);
        }

        public static WorkspaceUserJson Export (this WorkspaceUserData data, IDataStoreContext ctx)
        {
            var converter = ServiceContainer.Resolve<WorkspaceUserJsonConverter> ();
            return converter.Export (ctx, data);
        }
    }
}
