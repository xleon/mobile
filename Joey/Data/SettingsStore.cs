using System;
using System.Linq.Expressions;
using Android.Content;
using Toggl.Phoebe;
using Toggl.Phoebe.Data;
using XPlatUtils;

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

        private static string GetPropertyName<T> (Expression<Func<SettingsStore, T>> expr)
        {
            return expr.ToPropertyName ();
        }

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

        protected void OnSettingChanged (string name)
        {
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new SettingChangedMessage (this, name));
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

        public static readonly string PropertyGcmRegistrationId = GetPropertyName (s => s.GcmRegistrationId);

        public string GcmRegistrationId {
            get { return GetString (JoeyGcmRegistrationIdKey); }
            set {
                SetString (JoeyGcmRegistrationIdKey, value);
                OnSettingChanged (PropertyGcmRegistrationId);
            }
        }

        public static readonly string PropertyGcmAppVersion = GetPropertyName (s => s.GcmAppVersion);

        public int? GcmAppVersion {
            get { return GetInt (JoeyGcmAppVersionKey); }
            set {
                SetInt (JoeyGcmAppVersionKey, value);
                OnSettingChanged (PropertyGcmAppVersion);
            }
        }

        public static readonly string PropertyUserId = GetPropertyName (s => s.UserId);

        public Guid? UserId {
            get { return GetGuid (PhoebeUserIdKey); }
            set {
                SetGuid (PhoebeUserIdKey, value);
                OnSettingChanged (PropertyUserId);
            }
        }

        public static readonly string PropertyApiToken = GetPropertyName (s => s.ApiToken);

        public string ApiToken {
            get { return GetString (PhoebeApiTokenKey); }
            set {
                SetString (PhoebeApiTokenKey, value);
                OnSettingChanged (PropertyApiToken);
            }
        }

        public static readonly string PropertySyncLastRun = GetPropertyName (s => s.SyncLastRun);

        public DateTime? SyncLastRun {
            get { return GetDateTime (PhoebeSyncLastRunKey); }
            set {
                SetDateTime (PhoebeSyncLastRunKey, value);
                OnSettingChanged (PropertySyncLastRun);
            }
        }

        public static readonly string PropertyGotWelcomeMessage = GetPropertyName (s => s.GotWelcomeMessage);

        public bool GotWelcomeMessage {
            get { return GetInt (GotWelcomeMessageKey) == 1; }
            set {
                SetInt (GotWelcomeMessageKey, value ? 1 : 0);
                OnSettingChanged (PropertyGotWelcomeMessage);
            }
        }
    }
}
