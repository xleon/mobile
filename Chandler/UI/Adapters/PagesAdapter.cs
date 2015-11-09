using Android.App;
using Android.Content;
using Android.Support.Wearable.Views;
using Toggl.Chandler.UI.Fragments;

namespace Toggl.Chandler.UI.Adapters
{
    public class PagesAdapter : FragmentGridPagerAdapter
    {
        private readonly TimerFragment timerFragment = new TimerFragment();
        private readonly RecentsListFragment listFragment = new RecentsListFragment();
        private readonly OpenAppFragment openFragment = new OpenAppFragment();


        public PagesAdapter (Context ctx, FragmentManager fm) : base (fm)
        {
        }

        #region implemented abstract members of GridPagerAdapter

        public override int GetColumnCount (int p0)
        {
            return 3;
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
            } else if (col == 1) {
                return listFragment;
            } else {
                return openFragment;
            }
        }

        #endregion
    }
}
