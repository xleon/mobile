using System;
using UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace Toggl.Ross.Net
{
    public class NetworkIndicatorManager : IDisposable
    {
        public NetworkIndicatorManager ()
        {
            StoreManager.Singleton
                        .Observe (x => x.State.FullSyncResult.IsSyncing)
                        .DistinctUntilChanged ()
                        .ObserveOn (SynchronizationContext.Current)
                        .Subscribe (stateUpdated);

            setIndicator (false);
        }

        private void stateUpdated(bool isSyncing)
        {
            setIndicator (isSyncing);
        }

        private void setIndicator (bool isSyncing)
        {
            UIApplication.SharedApplication.NetworkActivityIndicatorVisible = isSyncing;
        }

        public void Dispose()
        {
            setIndicator (false);
        }

    }
}
