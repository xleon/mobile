using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace Toggl.Phoebe
{
    public static class ReactiveExtensions
    {
        public static IObservable<IList<T>> TimedBuffer<T>(
            this IObservable<T> observable, int milliseconds, Func<T, bool> filter = null)
        {
            return observable.Scan (
                seed: new List<T> (),
                accumulator: (acc, item) => {
                    if (filter == null || filter (item)) {
                        acc.Add (item);
                    }
                    return acc;
                })
                .Throttle (TimeSpan.FromMilliseconds (milliseconds));
        }
    }
}

