using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class JsonExtensions
    {
        public static async Task<CommonData> Import (this CommonJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var type = json.GetType ();
            if (type == typeof(ClientJson)) {
                return await Import ((ClientJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(ProjectJson)) {
                return await Import ((ProjectJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(ProjectUserJson)) {
                return await Import ((ProjectUserJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(TagJson)) {
                return await Import ((TagJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(TaskJson)) {
                return await Import ((TaskJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(TimeEntryJson)) {
                return await Import ((TimeEntryJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(UserJson)) {
                return await Import ((UserJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(WorkspaceJson)) {
                return await Import ((WorkspaceJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            } else if (type == typeof(WorkspaceUserJson)) {
                return await Import ((WorkspaceUserJson)json, localIdHint, forceUpdate).ConfigureAwait (false);
            }
            throw new InvalidOperationException (String.Format ("Unknown type of {0}", type));
        }

        public static async Task<CommonJson> Export (this CommonData data)
        {
            var type = data.GetType ();
            if (type == typeof(ClientData)) {
                return await Export ((ClientData)data).ConfigureAwait (false);
            } else if (type == typeof(ProjectData)) {
                return await Export ((ProjectData)data).ConfigureAwait (false);
            } else if (type == typeof(ProjectUserData)) {
                return await Export ((ProjectUserData)data).ConfigureAwait (false);
            } else if (type == typeof(TagData)) {
                return await Export ((TagData)data).ConfigureAwait (false);
            } else if (type == typeof(TaskData)) {
                return await Export ((TaskData)data).ConfigureAwait (false);
            } else if (type == typeof(TimeEntryData)) {
                return await Export ((TimeEntryData)data).ConfigureAwait (false);
            } else if (type == typeof(UserData)) {
                return await Export ((UserData)data).ConfigureAwait (false);
            } else if (type == typeof(WorkspaceData)) {
                return await Export ((WorkspaceData)data).ConfigureAwait (false);
            } else if (type == typeof(WorkspaceUserData)) {
                return await Export ((WorkspaceUserData)data).ConfigureAwait (false);
            }
            throw new InvalidOperationException (String.Format ("Unknown type of {0}", type));
        }

        public static Task<ClientData> Import (this ClientJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<ClientJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<ClientJson> Export (this ClientData data)
        {
            var converter = ServiceContainer.Resolve<ClientJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<ProjectData> Import (this ProjectJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<ProjectJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<ProjectJson> Export (this ProjectData data)
        {
            var converter = ServiceContainer.Resolve<ProjectJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<ProjectUserData> Import (this ProjectUserJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<ProjectUserJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<ProjectUserJson> Export (this ProjectUserData data)
        {
            var converter = ServiceContainer.Resolve<ProjectUserJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<TagData> Import (this TagJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<TagJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<TagJson> Export (this TagData data)
        {
            var converter = ServiceContainer.Resolve<TagJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<TaskData> Import (this TaskJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<TaskJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<TaskJson> Export (this TaskData data)
        {
            var converter = ServiceContainer.Resolve<TaskJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<TimeEntryData> Import (this TimeEntryJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<TimeEntryJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<TimeEntryJson> Export (this TimeEntryData data)
        {
            var converter = ServiceContainer.Resolve<TimeEntryJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<UserData> Import (this UserJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<UserJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<UserJson> Export (this UserData data)
        {
            var converter = ServiceContainer.Resolve<UserJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<WorkspaceData> Import (this WorkspaceJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<WorkspaceJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<WorkspaceJson> Export (this WorkspaceData data)
        {
            var converter = ServiceContainer.Resolve<WorkspaceJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<WorkspaceUserData> Import (this WorkspaceUserJson json, Guid? localIdHint = null, bool forceUpdate = false)
        {
            var converter = ServiceContainer.Resolve<WorkspaceUserJsonConverter> ();
            return converter.Import (json, localIdHint, forceUpdate);
        }

        public static Task<WorkspaceUserJson> Export (this WorkspaceUserData data)
        {
            var converter = ServiceContainer.Resolve<WorkspaceUserJsonConverter> ();
            return converter.Export (data);
        }
    }
}
