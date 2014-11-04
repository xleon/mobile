using System;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.Json;

namespace Toggl.Phoebe.Net
{
    public interface IReportsClient
    {
        Task<ReportJson> GetReports (DateTime startDate, DateTime endDate, long workspaceId);
    }
}

