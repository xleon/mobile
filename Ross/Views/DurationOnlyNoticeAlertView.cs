using UIKit;
using Toggl.Phoebe.Reactive;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data;

namespace Toggl.Ross.Views
{
    public class DurationOnlyNoticeAlertView : UIAlertView
    {
        public DurationOnlyNoticeAlertView () : base (
            "DurationOnlyNoticeTitle".Tr (),
            "DurationOnlyNoticeMessage".Tr (),
            null,
            "DurationOnlyNoticeOk".Tr ())
        {
        }

        public static bool TryShow (AppState state)
        {
            if (state.User == null || state.User.TrackingMode == TrackingMode.StartNew) {
                return false;
            }

            if (state.Settings.RossReadDurOnlyNotice) {
                return false;
            }

            var dia = new DurationOnlyNoticeAlertView ();
            dia.Clicked += delegate {
                RxChain.Send (new DataMsg.UpdateSetting (nameof (SettingsState.RossReadDurOnlyNotice), true));
            };
            dia.Show ();
            return true;
        }
    }
}
