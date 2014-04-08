using MonoTouch.UIKit;

namespace Toggl.Ross
{
    public class Application
    {
        static void Main (string[] args)
        {
            PixateFreestyleLib.PixateFreestyle.InitializePixateFreestyle ();
            UIApplication.Main (args, null, "AppDelegate");
        }
    }
}
