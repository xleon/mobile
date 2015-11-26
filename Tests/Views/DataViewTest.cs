using System;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Views;

namespace Toggl.Phoebe.Tests.Views
{
    public abstract class DataViewTest : Test
    {
        protected DateTime MakeTime (int hour, int minute, int second = 0)
        {
            return Time.UtcNow.Date
                   .AddHours (hour)
                   .AddMinutes (minute)
                   .AddSeconds (second);
        }

        protected async Task<T> GetByRemoteId<T> (long remoteId)
        where T : CommonData, new()
        {
            var rows = await DataStore.Table<T> ().Where (r => r.RemoteId == remoteId).ToListAsync ();
            return rows.Single ();
        }

        protected async Task ChangeData<T> (long remoteId, Action<T> modifier)
        where T : CommonData, new()
        {
            var model = await GetByRemoteId<T> (remoteId);
            modifier (model);
            await DataStore.PutAsync (model);
        }

        protected async Task WaitForLoaded<T> (IDataView<T> view)
        {
            if (!view.IsLoading) {
                return;
            }

            var tcs = new TaskCompletionSource<object> ();
            EventHandler onUpdated = null;

            onUpdated = delegate {
                if (view.IsLoading) {
                    return;
                }
                view.Updated -= onUpdated;
                tcs.SetResult (null);
            };

            view.Updated += onUpdated;
            await tcs.Task.ConfigureAwait (false);
        }

        protected async Task WaitForUpdates<T> (IDataView<T> view, int count = 1)
        {
            var tcs = new TaskCompletionSource<object> ();
            EventHandler onUpdated = null;

            onUpdated = delegate {
                if (--count > 0) {
                    return;
                }
                view.Updated -= onUpdated;
                tcs.TrySetResult (null);
            };

            view.Updated += onUpdated;
            await tcs.Task.ConfigureAwait (false);
        }
    }
}
