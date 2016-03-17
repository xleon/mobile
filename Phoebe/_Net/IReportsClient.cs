using System;
using System.Threading.Tasks;
using Toggl.Phoebe._Data.Json;

namespace Toggl.Phoebe._Net
{
    public interface IReportsClient
    {
        Task<ReportJson> GetReports (string authToken, long userRemoteId, DateTime startDate, DateTime endDate, long workspaceId);

        void CancelRequest ();

        bool IsCancellationRequested { get; }
    }
}

