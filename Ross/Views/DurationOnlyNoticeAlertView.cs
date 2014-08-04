using MonoTouch.UIKit;
using Toggl.Phoebe;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Ross.Data;

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

        public static bool TryShow ()
        {
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (authManager.User == null || authManager.User.TrackingMode == TrackingMode.StartNew)
                return false;

            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            if (settingsStore.ReadDurOnlyNotice)
                return false;

            var dia = new DurationOnlyNoticeAlertView ();
            dia.Clicked += delegate {
                settingsStore.ReadDurOnlyNotice = true;
            };
            dia.Show ();
            return true;
        }
    }
}
