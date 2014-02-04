using System;
using Android.Content;
using Android.OS;
using Android.Util;

namespace Toggl.Joey
{
    // Port of android.support.v4.content.WakefulBroadcastReceiver
    public abstract class WakefulBroadcastReceiver : BroadcastReceiver
    {
        private static readonly string ExtraWakeLockId = "com.toggl.android.wakelockid";
        private static readonly  SparseArray<PowerManager.WakeLock> ActiveWakeLocks =
            new SparseArray<PowerManager.WakeLock> ();
        private static int NextLockId = 1;

        public static ComponentName StartWakefulService (Context context, Intent intent)
        {
            lock (ActiveWakeLocks) {
                int id = NextLockId;
                NextLockId++;
                if (NextLockId <= 0) {
                    NextLockId = 1;
                }

                intent.PutExtra (ExtraWakeLockId, id);
                var comp = context.StartService (intent);
                if (comp == null) {
                    return null;
                }

                var pm = (PowerManager)context.GetSystemService (Context.PowerService);
                var wl = pm.NewWakeLock (WakeLockFlags.Partial,
                             "wake:" + comp.FlattenToShortString ());
                wl.SetReferenceCounted (false);
                wl.Acquire (60 * 1000);
                ActiveWakeLocks.Put (id, wl);

                return comp;
            }
        }

        public static bool CompleteWakefulIntent (Intent intent)
        {
            int id = intent.GetIntExtra (ExtraWakeLockId, 0);
            if (id == 0) {
                return false;
            }
            lock (ActiveWakeLocks) {
                var wl = ActiveWakeLocks.Get (id);
                if (wl != null) {
                    wl.Release ();
                    ActiveWakeLocks.Remove (id);
                    return true;
                }
                // We return true whether or not we actually found the wake lock
                // the return code is defined to indicate whether the Intent contained
                // an identifier for a wake lock that it was supposed to match.
                // We just log a warning here if there is no wake lock found, which could
                // happen for example if this function is called twice on the same
                // intent or the process is killed and restarted before processing the intent.
                Log.Warn ("WakefulBroadcastReceiver", "No active wake lock id #" + id);
                return true;
            }
        }
    }
}
