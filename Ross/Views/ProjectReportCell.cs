using System;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using System.Drawing;
using System.Diagnostics;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;

namespace Toggl.Ross.Views
{
    public class ProjectReportCell : UITableViewCell
    {
        public static NSString ProjectReportCellId = new NSString ("ProjectCellId");

        private ReportProject _data;
        public ReportProject Data
        {
            get {
                return _data;
            } set {
                _data = value;
                setProjectData ();
            }
        }

        UILabel projectTitleLabel;

        public ProjectReportCell (IntPtr handle) : base (handle)
        {
            projectTitleLabel = new UILabel ();
            ContentView.Add (projectTitleLabel);
            SelectionStyle = UITableViewCellSelectionStyle.None;
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();

            var contentFrame = ContentView.Frame;

            projectTitleLabel.Frame = new RectangleF (5, 5, 150, 25);
        }

        public override void SetSelected (bool selected, bool animated)
        {
            base.SetSelected (selected, animated);
            projectTitleLabel.Alpha = (selected) ? 1f : 0.5f;
        }

        private void setProjectData()
        {
            var hex = ProjectModel.HexColors [ _data.Color % ProjectModel.HexColors.Length];
            projectTitleLabel.TextColor = UIColor.Clear.FromHex ( hex);
            projectTitleLabel.Text = _data.Project + "   " + _data.TotalTime;
            projectTitleLabel.SetNeedsDisplay ();
        }
    }
}

