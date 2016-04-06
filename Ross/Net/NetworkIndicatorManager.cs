using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Reactive;
using UIKit;

namespace Toggl.Ross.Net
{
    public class NetworkIndicatorManager : IDisposable
    {
        IDisposable subscription;

        public NetworkIndicatorManager ()
        {
            subscription = StoreManager.Singleton
                        .Observe (x => x.State.RequestInfo.Running.Any (y => y is ServerRequest.GetChanges))
                        .DistinctUntilChanged ()
                        .ObserveOn (SynchronizationContext.Current)
                        .Subscribe (setIndicator);

            setIndicator (false);
        }

        private void setIndicator (bool isSyncing)
        {
            UIApplication.SharedApplication.NetworkActivityIndicatorVisible = isSyncing;
        }

        public void Dispose()
        {
            subscription.Dispose();
            setIndicator (false);
        }

    }
}
