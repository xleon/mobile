using System;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using PropertyChanged;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Phoebe._ViewModels
{
    public enum Mood {
        Neutral,
        Positive,
        Negative,
    }

    [ImplementPropertyChanged]
    public class FeedbackVM : ViewModelBase
    {
        private readonly AppState state;
        private const string Tag = "FeedbackMessage";

        public FeedbackVM (AppState state)
        {
            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Feedback";
            this.state = state;
        }

        public async Task<bool> Send (Mood currentMod, string message)
        {
            if (string.IsNullOrWhiteSpace (message)) {
                return false;
            }

            var json = new FeedbackJson () {
                Subject = "-- Toggl Mobile Feedback",
                IsMobile = true,
            };

            var sb = new StringBuilder ();
            sb.AppendLine ();
            sb.AppendLine (message);
            sb.AppendLine ();
            sb.AppendLine ("――――");
            sb.AppendLine ();

            AppendMood (currentMod, sb);
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

                await client.CreateFeedback (state.User.ApiToken, json).ConfigureAwait (false);
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

        private void AppendMood (Mood currentMood, StringBuilder sb)
        {
            sb.AppendFormat ("Mood: {0}", currentMood);
            sb.AppendLine ();
        }

        private void AppendTimeInfo (StringBuilder sb)
        {
            var timeManager = ServiceContainer.Resolve<TimeCorrectionManager> ();
            sb.AppendFormat ("Time correction: {0}", timeManager.Correction);
            sb.AppendLine ();
            sb.AppendFormat ("Time zone: {0}", Time.TimeZoneId);
            sb.AppendLine();
        }

        private async Task AppendTimeEntryStats (StringBuilder sb)
        {
            var userId = state.User.Id;
            var dataStore = ServiceContainer.Resolve<IDataStore> ();
            var total = await dataStore.Table<TimeEntryData> ()
                        .Where (r => r.UserId == userId)
                        .CountAsync().ConfigureAwait (false);
            var dirty = await dataStore.Table<TimeEntryData> ()
                        .Where (r => r.UserId == userId && r.SyncPending == true)
                        .CountAsync().ConfigureAwait (false);
            sb.AppendLine ("Time entries:");
            sb.AppendFormat (" - {0} total", total);
            sb.AppendLine ();
            sb.AppendFormat (" - {0} not synced", dirty);
            sb.AppendLine ();
            sb.AppendLine();
            sb.AppendLine();
        }
    }
}

