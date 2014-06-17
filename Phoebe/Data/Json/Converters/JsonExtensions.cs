using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public static class JsonExtensions
    {
        public static Task<ClientData> Import (this ClientJson json)
        {
            var converter = ServiceContainer.Resolve<ClientJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<ClientJson> Export (this ClientData data)
        {
            var converter = ServiceContainer.Resolve<ClientJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<ProjectData> Import (this ProjectJson json)
        {
            var converter = ServiceContainer.Resolve<ProjectJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<ProjectJson> Export (this ProjectData data)
        {
            var converter = ServiceContainer.Resolve<ProjectJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<ProjectUserData> Import (this ProjectUserJson json)
        {
            var converter = ServiceContainer.Resolve<ProjectUserJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<ProjectUserJson> Export (this ProjectUserData data)
        {
            var converter = ServiceContainer.Resolve<ProjectUserJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<TagData> Import (this TagJson json)
        {
            var converter = ServiceContainer.Resolve<TagJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<TagJson> Export (this TagData data)
        {
            var converter = ServiceContainer.Resolve<TagJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<TaskData> Import (this TaskJson json)
        {
            var converter = ServiceContainer.Resolve<TaskJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<TaskJson> Export (this TaskData data)
        {
            var converter = ServiceContainer.Resolve<TaskJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<TimeEntryData> Import (this TimeEntryJson json)
        {
            var converter = ServiceContainer.Resolve<TimeEntryJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<TimeEntryJson> Export (this TimeEntryData data)
        {
            var converter = ServiceContainer.Resolve<TimeEntryJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<UserData> Import (this UserJson json)
        {
            var converter = ServiceContainer.Resolve<UserJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<UserJson> Export (this UserData data)
        {
            var converter = ServiceContainer.Resolve<UserJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<WorkspaceData> Import (this WorkspaceJson json)
        {
            var converter = ServiceContainer.Resolve<WorkspaceJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<WorkspaceJson> Export (this WorkspaceData data)
        {
            var converter = ServiceContainer.Resolve<WorkspaceJsonConverter> ();
            return converter.Export (data);
        }

        public static Task<WorkspaceUserData> Import (this WorkspaceUserJson json)
        {
            var converter = ServiceContainer.Resolve<WorkspaceUserJsonConverter> ();
            return converter.Import (json);
        }

        public static Task<WorkspaceUserJson> Export (this WorkspaceUserData data)
        {
            var converter = ServiceContainer.Resolve<WorkspaceUserJsonConverter> ();
            return converter.Export (data);
        }
    }
}
