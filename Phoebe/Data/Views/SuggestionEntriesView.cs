using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json.Converters;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Gma.DataStructures.StringSearch;

namespace Toggl.Phoebe.Data.Views
{
    public class SuggestionEntriesView : IDataView<TimeEntryData>, IDisposable
    {
        private static readonly string Tag = "SuggestionEntriesView";
        private Subscription<DataChangeMessage> subscribitionDataChange;
        public bool IsLoading { get; private set; }
        public bool HasMore { get; private set; }
        public event EventHandler Updated;
        public readonly List<TimeEntryData> timeEntries = new List<TimeEntryData>();
        public readonly List<TimeEntryData> filteredEntries = new List<TimeEntryData> ();
        public string CurrentFilterSuffix { get; private set; }


        private ITrie<TimeEntryData> trie;

        public void Dispose()
        {

        }

        public SuggestionEntriesView (string baseFilterSuffix = "")
        {
            CurrentFilterSuffix = baseFilterSuffix;

            var bus = ServiceContainer.Resolve<MessageBus> ();
            Reload ();
            IsLoading = false;
        }

        private void ReinitTrie()
        {
            trie = new SuffixTrie<TimeEntryData> (2);
        }

        public IEnumerable<TimeEntryData> Data
        {
            get { return filteredEntries; }
        }

        public long Count
        {
            get { return filteredEntries.Count; }
        }

        public void LoadMore ()
        {
            Reload ();
        }

        public void Reload()
        {
            if (IsLoading) {
                return;
            }
            Load (true);
        }

        private async void Load (bool initialLoad)
        {
            if (IsLoading) {
                return;
            }
            IsLoading = true;

            ReinitTrie();

            try {
                var store = ServiceContainer.Resolve<IDataStore> ();
                var userId = ServiceContainer.Resolve<AuthManager> ().GetUserId ();
                var baseQuery = store.Table<TimeEntryData> ()
                                .OrderBy (r => r.StartTime, false)
                                .Where (r => r.State != TimeEntryState.New
                                        && r.DeletedAt == null
                                        && r.UserId == userId
                                        && r.Description != null);
                var entries = await baseQuery.QueryAsync ();
                timeEntries.AddRange (entries.ToList());
                foreach (var entry in timeEntries) {
                    trie.Add (entry.Description, entry);
                }

            } catch (Exception exc) {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error (Tag, exc, "Failed to fetch time entries");
            } finally {
                IsLoading = false;
                FilterBySuffix (CurrentFilterSuffix);
            }
        }

        public void FilterBySuffix (string suff)
        {
            if (!suff.Equals (CurrentFilterSuffix)) {
                CurrentFilterSuffix = suff;
            }

            filteredEntries.Clear ();
            filteredEntries.AddRange (trie.Retrieve (CurrentFilterSuffix));
            OnUpdated ();
        }

        private void OnUpdated ()
        {
            var handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }



    }
}

