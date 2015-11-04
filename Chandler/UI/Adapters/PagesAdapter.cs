using Android.App;
using Android.Content;
using Android.Support.Wearable.Views;
using Toggl.Chandler.UI.Fragments;
using System.Collections.Generic;
using System;

namespace Toggl.Chandler.UI.Adapters
{
    public class PagesAdapter : FragmentGridPagerAdapter
    {
        private TimerFragment timerFragment = new TimerFragment();
        private RecentsListFragment listFragment;// = new RecentsListFragment();

        public PagesAdapter (Context ctx, FragmentManager fm) : base (fm)
        {
            var dummyData = new List<SimpleTimeEntryData> ();
            dummyData.Add ( new SimpleTimeEntryData {
                Description = "Test",
                Project = "project",
                IsRunning = false,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow
            });
            dummyData.Add ( new SimpleTimeEntryData {
                Description = "Test",
                Project = "project",
                IsRunning = false,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow
            });
            dummyData.Add ( new SimpleTimeEntryData {
                Description = "Test",
                Project = "project",
                IsRunning = false,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow
            });
            dummyData.Add ( new SimpleTimeEntryData {
                Description = "Test",
                Project = "project",
                IsRunning = false,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow
            });
            dummyData.Add ( new SimpleTimeEntryData {
                Description = "Test",
                Project = "project",
                IsRunning = false,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow
            });
            dummyData.Add ( new SimpleTimeEntryData {
                Description = "Test",
                Project = "project",
                IsRunning = false,
                StartTime = DateTime.UtcNow,
                StopTime = DateTime.UtcNow
            });
            listFragment = new RecentsListFragment (dummyData);
        }

        #region implemented abstract members of GridPagerAdapter

        public override int GetColumnCount (int p0)
        {
            return 2;
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
            if (col == 0) {
                return timerFragment;
            }
            return listFragment;
        }

        #endregion
    }
}
