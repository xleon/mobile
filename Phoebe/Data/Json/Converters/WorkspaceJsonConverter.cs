using System;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Merge;
using XPlatUtils;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class WorkspaceJsonConverter : BaseJsonConverter
    {
        private const string Tag = "WorkspaceJsonConverter";

        public WorkspaceJson Export (IDataStoreContext ctx, WorkspaceData data)
        {
            return new WorkspaceJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt.ToUtc (),
                Name = data.Name,
                IsPremium = data.IsPremium,
                DefaultRate = data.DefaultRate,
                DefaultCurrency = data.DefaultCurrency,
                OnlyAdminsMayCreateProjects = data.ProjectCreationPrivileges == AccessLevel.Admin,
                OnlyAdminsSeeBillableRates = data.BillableRatesVisibility == AccessLevel.Admin,
                RoundingMode = data.RoundingMode,
                RoundingPercision = data.RoundingPercision,
                LogoUrl = data.LogoUrl,
            };
        }

        private static void ImportJson (WorkspaceData data, WorkspaceJson json)
        {
            data.Name = json.Name;
            data.IsPremium = json.IsPremium;
            data.DefaultRate = json.DefaultRate;
            data.DefaultCurrency = json.DefaultCurrency;
            data.ProjectCreationPrivileges = json.OnlyAdminsMayCreateProjects ? AccessLevel.Admin : AccessLevel.Regular;
            data.BillableRatesVisibility = json.OnlyAdminsSeeBillableRates ? AccessLevel.Admin : AccessLevel.Regular;
            data.RoundingMode = json.RoundingMode;
            data.RoundingPercision = json.RoundingPercision;
            data.LogoUrl = json.LogoUrl;

            ImportCommonJson (data, json);
        }

        public WorkspaceData Import (IDataStoreContext ctx, WorkspaceJson json, Guid? localIdHint = null, WorkspaceData mergeBase = null)
        {
            var log = ServiceContainer.Resolve<Logger> ();

            var data = GetByRemoteId<WorkspaceData> (ctx, json.Id.Value, localIdHint);

            var merger = mergeBase != null ? new WorkspaceMerger (mergeBase) : null;
            if (merger != null && data != null) {
                merger.Add (new WorkspaceData (data));
            }

            if (json.DeletedAt.HasValue) {
                if (data != null) {
                    log.Info (Tag, "Deleting local data for {0}.", data.ToIdString ());
                    ctx.Delete (data);
                    data = null;
                }
            } else if (merger != null || ShouldOverwrite (data, json)) {
                data = data ?? new WorkspaceData ();
                ImportJson (data, json);

                if (merger != null) {
                    merger.Add (data);
                    data = merger.Result;
                }

                if (merger != null) {
                    log.Info (Tag, "Importing {0}, merging with local data.", data.ToIdString ());
                } else {
                    log.Info (Tag, "Importing {0}, replacing local data.", data.ToIdString ());
                }

                data = ctx.Put (data);
            } else {
                log.Info (Tag, "Skipping import of {0}.", json.ToIdString ());
            }

            return data;
        }
    }
}
