using Android.App;
using Android.Content;
using Android.Support.Wearable.Views;

namespace Toggl.Chandler
{
    public class PagesAdapter : FragmentGridPagerAdapter
    {
        private readonly TimerFragment timerFragment = new TimerFragment();
        private readonly ListFragment listFragment = new ListFragment();

        public PagesAdapter (Context ctx, FragmentManager fm) : base (fm)
        {
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
