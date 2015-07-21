using CoreGraphics;
using Foundation;
using UIKit;

namespace Toggl.Ross.ViewControllers
{
    public partial class WebViewController : UIViewController
    {
        UIWebView webView;
        UIToolbar navBar;
        NSUrlRequest url;
        UIBarButtonItem [] items;

        public WebViewController (string url) : this (new NSUrl (url)) {}
        public WebViewController (NSUrl url) : this (new NSUrlRequest (url)) {}

        public WebViewController (NSUrlRequest url) : base ()
        {
            this.url = url;
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            navBar = new UIToolbar();
            navBar.Frame = new CGRect (0, View.Frame.Height-40, View.Frame.Width, 40);
            navBar.TintColor = UIColor.LightGray;

            items = new [] {
                new UIBarButtonItem (UIBarButtonSystemItem.Stop, (o, e) => {
                    webView.StopLoading ();
                    DismissViewController (true, null);
                }),
                new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace, null),
                new UIBarButtonItem (UIBarButtonSystemItem.Refresh, (o, e) => webView.Reload ())
            };

            navBar.Items = items;
            webView = new UIWebView ();
            webView.Frame = new CGRect (0, 0, View.Frame.Width, View.Frame.Height-40);
            webView.ScalesPageToFit = true;
            webView.SizeToFit ();
            webView.LoadRequest (url);

            navBar.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleTopMargin;
            webView.AutoresizingMask = UIViewAutoresizing.FlexibleDimensions;

            View.AddSubviews (new UIView[] { webView, navBar });
        }
    }
}

