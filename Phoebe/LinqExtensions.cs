using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;

namespace Toggl.Phoebe
{
    public static class LinqExtensions
    {
        public static IObservable<IList<T>> TimedBuffer<T> (this IObservable<T> observable, int milliseconds)
        {
            if (milliseconds > 0) {
                // TODO: This is firing up even if there're no events. Can it be improved?
                return observable
                       .Buffer (TimeSpan.FromMilliseconds (milliseconds))
                       .Where (b => b.Count > 0);
            } else {
                return observable
                .Select (x => new List<T> () { x });
            }
        }

        public static IEnumerable<T> ReplaceOrAppend<T> (this IEnumerable<T> items, T replacement, Func<T,bool> predicate)
        {
            var replaced = false;
            foreach (var item in items) {
                if (predicate (item)) {
                    replaced = true;
                    yield return replacement;
                } else {
                    yield return item;
                }
            }

            if (!replaced) {
                yield return replacement;
            }
        }

        public static bool SequenceEqual<T1, T2> (this IEnumerable<T1> seqA, IEnumerable<T2> seqB, Func<T1,T2, bool> compare)
        {
            var bothFinished = false;
            var enum1 = seqA.GetEnumerator ();
            var enum2 = seqB.GetEnumerator ();

            while (!bothFinished) {
                var firstFinished = !enum1.MoveNext ();
                var secondFinished = !enum2.MoveNext ();

                if (firstFinished && secondFinished) {
                    bothFinished = true;
                } else if (firstFinished || secondFinished) {
                    return false;
                } else {
                    if (!compare (enum1.Current, enum2.Current)) {
                        return false;
                    }
                }
            }

            return true;
        }


        public static IEnumerable<T> CollapsePairs<T> (this IEnumerable<T> items, Func<T,T,T> collapse)
        where T : class
        {
            T previous = null;
            foreach (var current in items)
            {
                if (previous == null) {
                    previous = current;
                } else {
                    var res = collapse (previous, current);
                    if (res == null) {
                        yield return previous;
                        previous = current;
                    } else {
                        yield return res;
                        previous = null;
                    }
                }
            }

            if (previous != null)
            {
                yield return previous;
            }
        }

        public static void ForEach<T> (this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items) {
                action (item);
            }
        }

        /// <summary>
        /// Returns the index of the first element fulfilling the predicate. -1 if none does.
        /// </summary>
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

