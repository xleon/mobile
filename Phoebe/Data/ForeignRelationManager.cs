using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using XPlatUtils;

namespace Toggl.Phoebe.Data
{
    public class ForeignRelationManager
    {
        public async Task<CommonData> ToListAsync (ForeignRelation relation)
        {
            var type = relation.Type;
            var id = relation.Id;

            if (type == typeof (ClientData)) {
                return await ToListAsync<ClientData> (id).ConfigureAwait (false);
            } else if (type == typeof (ProjectData)) {
                return await ToListAsync<ProjectData> (id).ConfigureAwait (false);
            } else if (type == typeof (ProjectUserData)) {
                return await ToListAsync<ProjectUserData> (id).ConfigureAwait (false);
            } else if (type == typeof (TagData)) {
                return await ToListAsync<TagData> (id).ConfigureAwait (false);
            } else if (type == typeof (TaskData)) {
                return await ToListAsync<TaskData> (id).ConfigureAwait (false);
            } else if (type == typeof (TimeEntryData)) {
                return await ToListAsync<TimeEntryData> (id).ConfigureAwait (false);
            } else if (type == typeof (TimeEntryTagData)) {
                return await ToListAsync<TimeEntryTagData> (id).ConfigureAwait (false);
            } else if (type == typeof (UserData)) {
                return await ToListAsync<UserData> (id).ConfigureAwait (false);
            } else if (type == typeof (WorkspaceData)) {
                return await ToListAsync<WorkspaceData> (id).ConfigureAwait (false);
            } else if (type == typeof (WorkspaceUserData)) {
                return await ToListAsync<WorkspaceUserData> (id).ConfigureAwait (false);
            }

            throw new InvalidOperationException (String.Format ("Unknown relation type {0}", type));
        }

        private async Task<T> ToListAsync<T> (Guid? id)
        where T : CommonData, new()
        {
            if (id == null) {
                return null;
            }

            var store = ServiceContainer.Resolve<IDataStore> ();
            var rows = await store.Table<T> ()
                       .Where (r => r.Id == id)
                       .ToListAsync().ConfigureAwait (false);
            return rows.FirstOrDefault ();
        }

        public IEnumerable<ForeignRelation> GetRelations (CommonData dataObject)
        {
            var type = dataObject.GetType ();
            if (type == typeof (ClientData)) {
                return GetClientRelations ((ClientData)dataObject);
            } else if (type == typeof (ProjectData)) {
                return GetProjectRelations ((ProjectData)dataObject);
            } else if (type == typeof (ProjectUserData)) {
                return GetProjectUserRelations ((ProjectUserData)dataObject);
            } else if (type == typeof (TagData)) {
                return GetTagRelations ((TagData)dataObject);
            } else if (type == typeof (TaskData)) {
                return GetTaskRelations ((TaskData)dataObject);
            } else if (type == typeof (TimeEntryData)) {
                return GetTimeEntryRelations ((TimeEntryData)dataObject);
            } else if (type == typeof (TimeEntryTagData)) {
                return GetTimeEntryTagRelations ((TimeEntryTagData)dataObject);
            } else if (type == typeof (UserData)) {
                return GetUserRelations ((UserData)dataObject);
            } else if (type == typeof (WorkspaceData)) {
                return GetWorkspaceRelations ((WorkspaceData)dataObject);
            } else if (type == typeof (WorkspaceUserData)) {
                return GetWorkspaceUserRelations ((WorkspaceUserData)dataObject);
            }
            throw new InvalidOperationException (String.Format ("Unable to determine relations for type {0}", type));
        }

        private IEnumerable<ForeignRelation> GetClientRelations (ClientData data)
        {
            yield return new ForeignRelation () {
                Name = "WorkspaceId",
                Type = typeof (WorkspaceData),
                Required = true,
                Id = data.WorkspaceId,
            };
        }

        private IEnumerable<ForeignRelation> GetProjectRelations (ProjectData data)
        {
            yield return new ForeignRelation () {
                Name = "WorkspaceId",
                Type = typeof (WorkspaceData),
                Required = true,
                Id = data.WorkspaceId,
            };
            yield return new ForeignRelation () {
                Name = "ClientId",
                Type = typeof (ClientData),
                Required = false,
                Id = data.ClientId,
            };
        }

        private IEnumerable<ForeignRelation> GetProjectUserRelations (ProjectUserData data)
        {
            yield return new ForeignRelation () {
                Name = "ProjectId",
                Type = typeof (ProjectData),
                Required = true,
                Id = data.ProjectId,
            };
            yield return new ForeignRelation () {
                Name = "UserId",
                Type = typeof (UserData),
                Required = true,
                Id = data.UserId,
            };
        }

        private IEnumerable<ForeignRelation> GetTagRelations (TagData data)
        {
            yield return new ForeignRelation () {
                Name = "WorkspaceId",
                Type = typeof (WorkspaceData),
                Required = true,
                Id = data.WorkspaceId,
            };
        }

        private IEnumerable<ForeignRelation> GetTaskRelations (TaskData data)
        {
            yield return new ForeignRelation () {
                Name = "WorkspaceId",
                Type = typeof (WorkspaceData),
                Required = true,
                Id = data.WorkspaceId,
            };
            yield return new ForeignRelation () {
                Name = "ProjectId",
                Type = typeof (ProjectData),
                Required = true,
                Id = data.ProjectId,
            };
        }

        private IEnumerable<ForeignRelation> GetTimeEntryRelations (TimeEntryData data)
        {
            yield return new ForeignRelation () {
                Name = "UserId",
                Type = typeof (UserData),
                Required = true,
                Id = data.UserId,
            };
            yield return new ForeignRelation () {
                Name = "WorkspaceId",
                Type = typeof (WorkspaceData),
                Required = true,
                Id = data.WorkspaceId,
            };
            yield return new ForeignRelation () {
                Name = "ProjectId",
                Type = typeof (ProjectData),
                Required = false,
                Id = data.ProjectId,
            };
            yield return new ForeignRelation () {
                Name = "TaskId",
                Type = typeof (TaskData),
                Required = false,
                Id = data.TaskId,
            };
        }

        private IEnumerable<ForeignRelation> GetTimeEntryTagRelations (TimeEntryTagData data)
        {
            yield return new ForeignRelation () {
                Name = "TimeEntryId",
                Type = typeof (TimeEntryData),
                Required = true,
                Id = data.TimeEntryId,
            };
            yield return new ForeignRelation () {
                Name = "TagId",
                Type = typeof (TagData),
                Required = true,
                Id = data.TagId,
            };
        }

        private IEnumerable<ForeignRelation> GetUserRelations (UserData data)
        {
            yield return new ForeignRelation () {
                Name = "DefaultWorkspaceId",
                Type = typeof (WorkspaceData),
                Required = true,
                Id = data.DefaultWorkspaceId,
            };
        }

        private IEnumerable<ForeignRelation> GetWorkspaceRelations (WorkspaceData data)
        {
            return Enumerable.Empty<ForeignRelation> ();
        }

        private IEnumerable<ForeignRelation> GetWorkspaceUserRelations (WorkspaceUserData data)
        {
            yield return new ForeignRelation () {
                Name = "WorkspaceId",
                Type = typeof (WorkspaceData),
                Required = true,
                Id = data.WorkspaceId,
            };
            yield return new ForeignRelation () {
                Name = "UserId",
                Type = typeof (UserData),
                Required = true,
                Id = data.UserId,
            };
        }
    }
}
