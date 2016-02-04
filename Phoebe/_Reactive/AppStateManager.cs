using System;
using System.Reactive.Linq;
using Toggl.Phoebe._Helpers;

namespace Toggl.Phoebe._Reactive
{
    public class AppStateManager
    {
        AppState state;
        CompositeUpdater<AppState> updater;
        event EventHandler<IAppState> notify;

        public static AppStateManager Singleton { get; private set; }

		public static void Init ()
		{
            Singleton = Singleton ?? new AppStateManager ();
		}

        public IAppState GetState ()
        {
            return state;
        }

        AppStateManager ()
        {
            // TODO: Initialize state and updater

            StoreManager.Singleton.Observe().Subscribe (
                msg => {
                    // TODO: Error management
                    updater.Update (state, msg);
                    notify.SafeInvoke (this, state);
                });
        }

        public IObservable<T> Observe<T> (Func<IAppState, T> selector)
        {
            return Observable.FromEventPattern<IAppState> (
                h => notify += h,
                h => notify -= h
            )
            .Select (ev => selector (ev.EventArgs))
            .DistinctUntilChanged ();
        }
	}
}

