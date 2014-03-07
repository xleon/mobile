using System;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;

namespace Toggl.Joey.Data
{
    /// <summary>
    /// Settings store for storing Android runtime settings. It also stores settings for Phoebe, but the
    /// <see cref="ISettingsStore"/> interface is implemented explicitly.
    /// </summary>
    public class SettingsStore : ISettingsStore
    {
        private const string PreferenceName = "togglSettings";
        private const string PhoebeUserIdKey = "phoebeUserId";
        private const string PhoebeApiTokenKey = "phoebeApiToken";
        private const string PhoebeSyncLastRunKey = "phoebeSyncLastRun";
        private const string JoeyInstallIdKey = "joeyInstallId";
        private const string JoeyGcmRegistrationIdKey = "joeyGcmRegistrationId";
        private const string JoeyGcmAppVersionKey = "joeyGcmAppVersion";
        private const string GotWelcomeMessageKey = "gotWelcomeMessage";
        private readonly ISharedPreferences prefs;

        public SettingsStore (Context ctx)
        {
            prefs = ctx.GetSharedPreferences (PreferenceName, FileCreationMode.Private);
        }

        protected Guid? GetGuid (string key)
        {
            var val = prefs.GetString (key, null);
            if (String.IsNullOrEmpty (val))
                return null;
            return Guid.Parse (val);
        }

        protected void SetGuid (string key, Guid? value)
        {
            if (value != null) {
                prefs.Edit ().PutString (key, value.Value.ToString ()).Commit ();
            } else {
                prefs.Edit ().Remove (key).Commit ();
            }
        }

        protected string GetString (string key)
        {
            return prefs.GetString (key, null);
        }

        protected void SetString (string key, string value)
        {
            if (value != null) {
                prefs.Edit ().PutString (key, value).Commit ();
            } else {
                prefs.Edit ().Remove (key).Commit ();
            }
        }

        protected int? GetInt (string key)
        {
            if (!prefs.Contains (key)) {
                return null;
            } else {
                return prefs.GetInt (key, 0);
            }
        }

        protected void SetInt (string key, int? value)
        {
            if (value != null) {
                prefs.Edit ().PutInt (key, value.Value).Commit ();
            } else {
                prefs.Edit ().Remove (key).Commit ();
            }
        }

        protected DateTime? GetDateTime (string key)
        {
            if (!prefs.Contains (key)) {
                return null;
            } else {
                return DateTime.FromBinary (prefs.GetLong (key, 0)).ToUtc ();
            }
        }

        protected void SetDateTime (string key, DateTime? value)
        {
            if (value != null) {
                prefs.Edit ().PutLong (key, value.Value.ToBinary ()).Commit ();
            } else {
                prefs.Edit ().Remove (key).Commit ();
            }
        }

        public string InstallId {
            get {
                var val = GetString (JoeyInstallIdKey);
                if (String.IsNullOrEmpty (val)) {
                    val = Guid.NewGuid ().ToString ();
                    SetString (JoeyInstallIdKey, val);
                }
                return val;
            }
        }

        public string GcmRegistrationId {
            get { return GetString (JoeyGcmRegistrationIdKey); }
            set { SetString (JoeyGcmRegistrationIdKey, value); }
        }

        public int? GcmAppVersion {
            get { return GetInt (JoeyGcmAppVersionKey); }
            set { SetInt (JoeyGcmAppVersionKey, value); }
        }

        Guid? ISettingsStore.UserId {
            get { return GetGuid (PhoebeUserIdKey); }
            set { SetGuid (PhoebeUserIdKey, value); }
        }

        string ISettingsStore.ApiToken {
            get { return GetString (PhoebeApiTokenKey); }
            set { SetString (PhoebeApiTokenKey, value); }
        }

        DateTime? ISettingsStore.SyncLastRun {
            get { return GetDateTime (PhoebeSyncLastRunKey); }
            set { SetDateTime (PhoebeSyncLastRunKey, value); }
        }

        public bool GotWelcomeMessage {
            get { return GetInt (GotWelcomeMessageKey) == 1; }
            set { SetInt (GotWelcomeMessageKey, value ? 1 : 0); }
        }
    }
}
