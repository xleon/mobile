using System;
using System.Drawing;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
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

                var hex = ProjectModel.HexColors [ _data.Color % ProjectModel.HexColors.Length];
                circleView.BackgroundColor = UIColor.Clear.FromHex ( hex);
                projectTitleLabel.Text = _data.Project;
                timeLabel.Text = _data.FormattedTime;
                projectTitleLabel.SetNeedsDisplay ();
            }
        }

        UILabel projectTitleLabel;
        UILabel timeLabel;
        UIView circleView;

        public ProjectReportCell (IntPtr handle) : base (handle)
        {
            SelectionStyle = UITableViewCellSelectionStyle.None;

            projectTitleLabel = new UILabel ();
            projectTitleLabel.Apply (Style.ReportsView.ProjectCellTitleLabel);
            ContentView.Add (projectTitleLabel);

            timeLabel = new UILabel ();
            timeLabel.Apply (Style.ReportsView.ProjectCellTimeLabel);
            ContentView.Add (timeLabel);

            circleView = new UIView ();
            ContentView.Add (circleView);
        }

        public override void LayoutSubviews ()
        {
            base.LayoutSubviews ();
            const float radius = 17.0f;
            var contentFrame = ContentView.Frame;

            projectTitleLabel.Frame = new RectangleF ( radius + radius * 0.5f, (contentFrame.Height - 20)/2, 250, 20);
            timeLabel.Frame = new RectangleF ( contentFrame.Width - 100, (contentFrame.Height - 20)/2, 100, 20);
            circleView.Frame = new RectangleF (0, (contentFrame.Height - radius) / 2, radius, radius);
            circleView.Layer.CornerRadius = 5;
        }

        public override void SetSelected (bool selected, bool animated)
        {
            base.SetSelected (selected, animated);
            projectTitleLabel.Alpha = (selected) ? 1f : 0.5f;
            circleView.Layer.CornerRadius = (selected) ? circleView.Frame.Width/2 : 5;
        }

        private string FormatMilliseconds (long ms)
        {
            var timeSpan = TimeSpan.FromMilliseconds (ms);
            decimal totalHours = Math.Floor ((decimal)timeSpan.TotalHours);
            return String.Format ("{0} h {1} min", (int)totalHours, timeSpan.Minutes);
        }
    }
}

