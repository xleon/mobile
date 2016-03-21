using System;
using System.Text;
using System.Threading.Tasks;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe.Net
{
    public class FeedbackMessage
    {
        private const string Tag = "FeedbackMessage";

        public Mood CurrentMood { get; set; }

        public string Message { get; set; }

        public async Task<bool> Send ()
        {
            if (String.IsNullOrWhiteSpace (Message)) {
                return false;
            }

            var json = new FeedbackJson () {
                Subject = "-- Toggl Mobile Feedback",
                IsMobile = true,
            };

            var sb = new StringBuilder ();
            sb.AppendLine ();
            sb.AppendLine (Message);
            sb.AppendLine ();
            sb.AppendLine ("――――");
            sb.AppendLine ();

            AppendMood (sb);
            AppendTimeInfo (sb);
            await AppendTimeEntryStats (sb).ConfigureAwait (false);

            var client = ServiceContainer.Resolve<ITogglClient> ();
            var logStore = ServiceContainer.Resolve<LogStore> ();
            try {
                json.Message = sb.ToString ();
                json.AttachmentData = await logStore.Compress ().ConfigureAwait (false);
                if (json.AttachmentData != null) {
                    json.AttachmentName = "log.gz";
                }

                await client.CreateFeedback (json).ConfigureAwait (false);
            } catch (Exception ex) {
                var log = ServiceContainer.Resolve<ILogger> ();
                if (ex.IsNetworkFailure ()) {
                    log.Info (Tag, ex, "Failed to send feedback.");
                } else {
                    log.Warning (Tag, ex, "Failed to send feedback.");
                }
                return false;
            }

            return true;
        }

        private void AppendMood (StringBuilder sb)
        {
            sb.AppendFormat ("Mood: {0}", CurrentMood);
            sb.AppendLine ();
        }

        private void AppendTimeInfo (StringBuilder sb)
        {
            var timeManager = ServiceContainer.Resolve<TimeCorrectionManager> ();
            sb.AppendFormat ("Time correction: {0}", timeManager.Correction);
            sb.AppendLine ();
            sb.AppendFormat ("Time zone: {0}", Time.TimeZoneId);
            sb.AppendLine ();
        }

        private async Task AppendTimeEntryStats (StringBuilder sb)
        {
            var userId = StoreManager.Singleton.AppState.User.Id;
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var total = await dataStore.Table<TimeEntryData> ()
                        .Where (r => r.UserId == userId)
                        .CountAsync().ConfigureAwait (false);
            var dirty = await dataStore.Table<TimeEntryData> ()
                        .Where (r => r.UserId == userId && r.IsDirty == true && r.RemoteRejected == false)
                        .CountAsync().ConfigureAwait (false);
            var rejected = await dataStore.Table<TimeEntryData> ()
                           .Where (r => r.UserId == userId && r.RemoteRejected == true)
                           .CountAsync().ConfigureAwait (false);

            sb.AppendLine ("Time entries:");
            sb.AppendFormat (" - {0} total", total);
            sb.AppendLine ();
            sb.AppendFormat (" - {0} not synced", dirty);
            sb.AppendLine ();
            sb.AppendFormat (" - {0} rejected", rejected);
            sb.AppendLine ();
            sb.AppendLine ();
        }

        public enum Mood {
            Neutral,
            Positive,
            Negative,
        }
    }
}
