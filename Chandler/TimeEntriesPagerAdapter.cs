using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Support.Wearable.Views;

namespace Toggl.Chandler
{
    public class TimeEntriesPagerAdapter : FragmentGridPagerAdapter
    {
        private Context mContext;
        private List<SimpleTimeEntryData> timeEntries = new List<SimpleTimeEntryData> ();

        public TimeEntriesPagerAdapter (Context ctx, FragmentManager fm) : base (fm)
        {
            mContext = ctx;
            timeEntries.Add (new SimpleTimeEntryData {
                Project = "Wearable",
                Description = "Dev mode",
                StartTime = DateTime.UtcNow,
                IsRunning = true
            });
        }

        public void UpdateEntries (List<SimpleTimeEntryData> data)
        {
            Console.WriteLine ("Update entries");
            timeEntries = data;
            NotifyDataSetChanged();
        }

        #region implemented abstract members of GridPagerAdapter

        public override int GetColumnCount (int p0)
        {
            return timeEntries.Count;
        }

        public override int RowCount
        {
            get {
                return 1;
            }
        }

        #endregion

        #region implemented abstract members of FragmentGridPagerAdapter

        public override Fragment GetFragment (int row, int col)
        {
            var fragment =  new TimeEntryFragment (timeEntries[col]);
            return fragment;
        }

        #endregion
    }
}
