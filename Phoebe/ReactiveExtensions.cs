using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace Toggl.Phoebe
{
    public static class ReactiveExtensions
    {
        public static IObservable<IList<T>> TimedBuffer<T> (this IObservable<T> observable, int milliseconds)
        {
            // TODO: This is firing up even if there're no events. Can it be improved?
            return observable
                .Buffer (TimeSpan.FromMilliseconds (milliseconds))
                .Where (b => b.Count > 0);
        }

        public static void ForEach<T> (this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items) {
                action (item);
            }
        }

        public static int IndexOf<T> (this IEnumerable<T> items, Func<T, bool> predicate)
        {
            var i = 0;
            foreach (var item in items) {
                if (predicate (item)) {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public static IEnumerable<T> Prepend<T> (this IEnumerable<T> items, T head)
        {
            yield return head;
            foreach (var item in items) {
                yield return item;
            }
        }

        public static IEnumerable<T> Append<T> (this IEnumerable<T> items, T tail)
        {
            foreach (var item in items) {
                yield return item;
            }
            yield return tail;
        }
    }
}

