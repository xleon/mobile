using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace TogglDoodle.Models
{
    public class TimeEntryModel : Model
    {
        public static long NextId {
            get { return Model.NextId<TimeEntryModel> (); }
        }

        private readonly int workspaceRelationId;
        private readonly int projectRelationId;
        private readonly int taskRelationId;

        public TimeEntryModel ()
        {
            workspaceRelationId = ForeignRelation (() => WorkspaceId, () => Workspace);
            projectRelationId = ForeignRelation (() => ProjectId, () => Project);
            taskRelationId = ForeignRelation (() => TaskId, () => Task);
        }

        public string Description { get; set; }

        public bool Billable { get; set; }

        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        public long Duration { get; set; }

        public string CreatedWith { get; set; }

        public ISet<string> Tags { get { return null; } }

        public bool DurationOnly { get; set; }

        #region Relations

        public long? WorkspaceId {
            get { return GetForeignId (workspaceRelationId); }
            set { SetForeignId (workspaceRelationId, value); }
        }

        [SQLite.Ignore]
        public WorkspaceModel Workspace {
            get { return GetForeignModel<WorkspaceModel> (workspaceRelationId); }
            set { SetForeignModel (workspaceRelationId, value); }
        }

        public long? ProjectId {
            get { return GetForeignId (projectRelationId); }
            set { SetForeignId (projectRelationId, value); }
        }

        [SQLite.Ignore]
        public ProjectModel Project {
            get { return GetForeignModel<ProjectModel> (projectRelationId); }
            set { SetForeignModel (projectRelationId, value); }
        }

        public long? TaskId {
            get { return GetForeignId (taskRelationId); }
            set { SetForeignId (taskRelationId, value); }
        }

        [SQLite.Ignore]
        public TaskModel Task {
            get { return GetForeignModel<TaskModel> (taskRelationId); }
            set { SetForeignModel (taskRelationId, value); }
        }

        #endregion

    }
}
