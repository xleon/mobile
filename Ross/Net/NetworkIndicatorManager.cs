using System;
using System.Reactive.Linq;
using System.Threading;
using Toggl.Phoebe.Reactive;
using UIKit;

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
                        .Subscribe (setIndicator);

            setIndicator (false);
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
