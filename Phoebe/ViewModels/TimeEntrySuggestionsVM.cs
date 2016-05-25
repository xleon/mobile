using System;
using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Phoebe.Logging;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Phoebe.Data.Models;
using VDS.Common.Tries;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Helpers;
using Toggl.Phoebe.ViewModels.Timer;

namespace Toggl.Phoebe.ViewModels
{
    public class TimeEntrySuggestionsVM : ObservableRangeCollection<IHolder>, IDisposable
    {
        private static readonly string Tag = "SuggestionEntriesView";
        public bool IsLoading { get; private set; }
        public bool HasMore { get; private set; }

        public event EventHandler Updated;

        public readonly List<TimeEntryData> TimeEntries = new List<TimeEntryData>();
        public readonly List<TimeEntryData> FilteredEntries = new List<TimeEntryData> ();

        private string currentFilterInfix = "";
        private StringTrie<TimeEntryData> trie;

        public TimeEntrySuggestionsVM()
        {
            Reload();
        }

        public bool HasSuggestions
        {
            get { return FilteredEntries != null && FilteredEntries.Count > 0; }
        }

        private void ReinitTrie()
        {
            trie = new StringTrie<TimeEntryData> ();
        }

        public IEnumerable<TimeEntryData> Data
        {
            get { return FilteredEntries; }
        }

        public long Count
        {
            get { return FilteredEntries.Count; }
        }

        public void LoadMore()
        {
            Reload();
        }

        public void Reload()
        {
            Load();
        }

        private async void Load()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;

            ReinitTrie();

            try
            {
                var dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
//                dataStore.Table<TimeEntryData> ().OrderBy(
                var entries = dataStore.Table<TimeEntryData> ()
                              .OrderBy(r => r.StartTime)
                              .Where(r => r.State != TimeEntryState.New
                                     && r.DeletedAt == null
                                     && r.Description != null)
                              .ToList();
                TimeEntries.AddRange(entries);
                foreach (var entry in TimeEntries)
                {
                    trie.Add(entry.Description.ToLower(), entry);
                }

            }
            catch (Exception exc)
            {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Error(Tag, exc, "Failed to fetch time entries");
            }
            finally
            {
                IsLoading = false;
                FilterByInfix(currentFilterInfix);
            }
        }

        public void FilterByInfix(string suff)
        {
            var lowerSuff = suff.ToLower();
            if (!lowerSuff.Equals(currentFilterInfix))
            {
                currentFilterInfix = lowerSuff;
            }

            FilteredEntries.Clear();
            if (currentFilterInfix.Length < 3)
            {
                OnUpdated();
                return;
            }

            var result = trie.Find(currentFilterInfix);
            FilteredEntries.AddRange(result.Values);
            OnUpdated();
        }

        public void Dispose()
        {
        }

        private void OnUpdated()
        {
            var handler = Updated;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
