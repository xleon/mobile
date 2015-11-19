using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace Toggl.Phoebe
{
    public static class ReactiveExtensions
    {
        /// <summary>
        /// Using Buffer will trigger the observable every timespan even if the feed is not pushing anything. 
        /// This makes sure the chain is continued only when:
        /// 1) At least one item has been received from the source
        /// 2) The number of specified milliseconds has passed
        /// </summary>
        /// <param name="milliseconds">Buffer timespan in milliseconds</param>
        public static IObservable<IList<T>> TimedBuffer<T>(this IObservable<T> observable, int milliseconds)
        {
            return observable
                .Scan (new List<T> (), (acc, item) => {
                    acc.Add (item);
                    return acc;
                })
                .Throttle (TimeSpan.FromMilliseconds (milliseconds));
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items)
                action(item);
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> items, T head)
        {
            yield return head;
            foreach (var item in items)
                yield return item;
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> items, T tail)
        {
            foreach (var item in items)
                yield return item;
			yield return tail;
        }
    }
}

