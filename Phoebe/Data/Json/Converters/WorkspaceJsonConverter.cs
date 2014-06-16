using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;

namespace Toggl.Phoebe.Data.Json.Converters
{
    public sealed class WorkspaceJsonConverter : BaseJsonConverter
    {
        public Task<WorkspaceJson> Export (WorkspaceData data)
        {
            return Task.FromResult (new WorkspaceJson () {
                Id = data.RemoteId,
                ModifiedAt = data.ModifiedAt,
                Name = data.Name,
                IsPremium = data.IsPremium,
                DefaultRate = data.DefaultRate,
                DefaultCurrency = data.DefaultCurrency,
                OnlyAdminsMayCreateProjects = data.ProjectCreationPrivileges == AccessLevel.Admin,
                OnlyAdminsSeeBillableRates = data.BillableRatesVisibility == AccessLevel.Admin,
                RoundingMode = data.RoundingMode,
                RoundingPercision = data.RoundingPercision,
                LogoUrl = data.LogoUrl,
            });
        }

        private static void Merge (WorkspaceData data, WorkspaceJson json)
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

            MergeCommon (data, json);
        }

        public async Task<WorkspaceData> Import (WorkspaceJson json)
        {
            var data = await GetByRemoteId<WorkspaceData> (json.Id.Value).ConfigureAwait (false);

            if (data == null || data.ModifiedAt < json.ModifiedAt) {
                if (json.DeletedAt == null) {
                    data = data ?? new WorkspaceData ();
                    Merge (data, json);
                    data = await DataStore.PutAsync (data).ConfigureAwait (false);
                } else if (data != null) {
                    await DataStore.DeleteAsync (data).ConfigureAwait (false);
                    data = null;
                }
            }

            return data;
        }
    }
}
