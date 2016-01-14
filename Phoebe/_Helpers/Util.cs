using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Helpers
{
    public static class Util
    {
        public static T Rethrow<T> (Exception ex)
        {
            throw ex;
        }

        public static T Unexpected<T> (object value = null)
        {
            throw value != null ? new UnexpectedException (value) : new UnexpectedException ();
        }

        public static void LogError (string tag, Exception ex, string msg)
        {
            var log = ServiceContainer.Resolve<ILogger> ();
            log.Error (tag, ex, msg);
        }

        public static async Task<T> TryCatchAsync<T> (Func<Task<T>> @try, Func<Exception,T> @catch)
        {
            try {
                return await @try();
            } catch (Exception ex) {
                return @catch (ex);
            }
        }

        public static Task<bool> AwaitPredicate (Func<bool> predicate, double interval = 100, double timeout = 5000)
        {
            var tcs = new TaskCompletionSource<bool> ();

            double timePassed = 0;
            var timer = new System.Timers.Timer (interval)  { AutoReset = true };
            timer.Elapsed += (s, e) => {
                timePassed += interval;
                if (timePassed >= timeout) {
                    timer.Stop ();
                    tcs.SetResult (false);
                } else {
                    var success = predicate ();
                    if (success) {
                        timer.Stop ();
                        tcs.SetResult (true);
                    }
                }
            };
            timer.Start ();

            return tcs.Task;
        }

        public static EitherGroup<TL,TR> Split<TL,TR> (this IEnumerable<Either<TL,TR>> items)
        {
            var leftList = new List<TL> ();
            var rightList = new List<TR> ();
            foreach (var item in items) {
                item.Match (leftList.Add, rightList.Add);
            }
            return new EitherGroup<TL, TR> (leftList, rightList);
        }
    }

    public class UnexpectedException : Exception
    {
        public object Value { get; private set; }

        public UnexpectedException (object value)
        : base (string.Format ("Unexpected value: {0}", value))
        {
            Value = value;
        }

        public UnexpectedException () : base ("Unexpected") { }
    }

    public class Either<TL,TR>
    {
        readonly TL _left;
        readonly TR _right;
        readonly bool _isLeft;

        Either (bool isLeft, TL left, TR right)
        {
            _isLeft = isLeft;
            _left = left;
            _right = right;
        }

        public static Either<TL,TR> Left (TL left)
        {
            return new Either<TL, TR> (true, left, default (TR));
        }

        public static Either<TL,TR> Right (TR right)
        {
            return new Either<TL, TR> (false, default (TL), right);
        }

        public T Match<T> (Func<TL,T> left, Func<TR,T> right)
        {
            return _isLeft ? left (_left) : right (_right);
        }

        public void Match (Action<TL> left, Action<TR> right)
        {
            if (_isLeft) {
                left (_left);
            } else {
                right (_right);
            }
        }

        public Either<TL2,TR2> Select<TL2,TR2> (Func<TL,TL2> left, Func<TR,TR2> right)
        {
            return _isLeft ? Either<TL2,TR2>.Left (left (_left)) : Either<TL2,TR2>.Right (right (_right));
        }
    }

    public class EitherGroup<TL,TR>
    {
        public IList<TL> Left { get; private set; }
        public IList<TR> Right { get; private set; }

        public EitherGroup (IList<TL> left, IList<TR> right)
        {
            Left = left;
            Right = right;
        }
    }
}
