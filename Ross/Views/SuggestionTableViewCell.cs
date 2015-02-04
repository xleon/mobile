using System;
using System.Drawing;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;
using Cirrious.FluentLayouts.Touch;

namespace Toggl.Ross.Views
{
    public class SuggestionTableViewCell : ModelTableViewCell<TimeEntryModel>
    {
        private const float HorizPadding = 15.0f;
        private readonly UIView textContentView;
        private readonly UILabel projectLabel;
        private readonly UILabel clientLabel;
        private readonly UILabel descriptionLabel;

        public SuggestionTableViewCell (IntPtr ptr) : base (ptr)
        {
            textContentView = new UIView ();
            projectLabel = new UILabel ().Apply (Style.Recent.CellProjectLabel);
            clientLabel = new UILabel ().Apply (Style.Recent.CellClientLabel);
            descriptionLabel = new UILabel ().Apply (Style.Recent.CellDescriptionLabel);

            textContentView.AddSubviews (projectLabel, clientLabel, descriptionLabel);

            ContentView.AddSubview (textContentView);

            ContentView.AddConstraints (new FluentLayout[] {
                textContentView.AtTopOf (ContentView),
                textContentView.AtBottomOf (ContentView),
                textContentView.AtLeftOf (ContentView),
                textContentView.AtRightOf (ContentView)
            } .ToLayoutConstraints());

            textContentView.AddConstraints (new FluentLayout[] {
                projectLabel.AtTopOf (textContentView, 8),
                projectLabel.AtLeftOf (textContentView, HorizPadding),
                clientLabel.WithSameCenterY (projectLabel),
                clientLabel.AtLeftOf (projectLabel),
                clientLabel.AtRightOf (textContentView, HorizPadding),
                descriptionLabel.Below (projectLabel, 4),
                descriptionLabel.AtLeftOf (textContentView, HorizPadding+1),
                descriptionLabel.AtRightOf (textContentView, HorizPadding)
            } .ToLayoutConstraints());

            textContentView.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();
            ContentView.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();
        }

        protected override void OnDataSourceChanged ()
        {
            Rebind ();
            base.OnDataSourceChanged ();
        }

        protected override void Rebind ()
        {
            ResetTrackedObservables ();

            if (DataSource == null) {
                return;
            }

            var model = DataSource;

            var projectName = "LogCellNoProject".Tr ();
            var projectColor = Color.Gray;
            var clientName = String.Empty;

            if (model.Project != null) {
                projectName = model.Project.Name;
                projectColor = UIColor.Clear.FromHex (model.Project.GetHexColor ());

                if (model.Project.Client != null) {
                    clientName = model.Project.Client.Name;
                }
            } else {
                Console.WriteLine ("project model is null");
            }

            projectLabel.TextColor = projectColor;

            Console.WriteLine ("Checking {0}", projectName);
            if (projectLabel.Text != projectName) {
                Console.WriteLine ("Setting project label text {0}", projectName);
                projectLabel.Text = projectName;
                projectLabel.InvalidateIntrinsicContentSize ();
                SetNeedsLayout ();
            }

            if (clientLabel.Text != clientName) {
                clientLabel.Text = clientName;
                clientLabel.InvalidateIntrinsicContentSize ();
                SetNeedsLayout ();
            }

            var description = model.Description;
            var descriptionHidden = String.IsNullOrWhiteSpace (description);

            if (descriptionHidden) {
                description = "LogCellNoDescription".Tr ();
                descriptionHidden = false;
            }

            if (descriptionLabel.Text != description) {
                descriptionLabel.Text = description;
                descriptionLabel.InvalidateIntrinsicContentSize ();
                SetNeedsLayout ();
            }



            LayoutIfNeeded ();
        }

        private void HandleTimeEntryPropertyChanged (string prop)
        {
            if (prop == TimeEntryModel.PropertyProject
                    || prop == TimeEntryModel.PropertyTask
                    || prop == TimeEntryModel.PropertyStartTime
                    || prop == TimeEntryModel.PropertyStopTime
                    || prop == TimeEntryModel.PropertyState
                    || prop == TimeEntryModel.PropertyIsBillable
                    || prop == TimeEntryModel.PropertyDescription) {
                Rebind ();
            }
        }

        private void HandleProjectPropertyChanged (string prop)
        {
            if (prop == ProjectModel.PropertyClient
                    || prop == ProjectModel.PropertyName
                    || prop == ProjectModel.PropertyColor) {
                Rebind ();
            }
        }

        private void HandleClientPropertyChanged (string prop)
        {
            if (prop == ClientModel.PropertyName) {
                Rebind ();
            }
        }

        private void HandleTaskPropertyChanged (string prop)
        {
            if (prop == TaskModel.PropertyName) {
                Rebind ();
            }
        }

        protected override void ResetTrackedObservables ()
        {
            Tracker.MarkAllStale ();

            if (DataSource != null) {
                Tracker.Add (DataSource, HandleTimeEntryPropertyChanged);

                if (DataSource.Project != null) {
                    Tracker.Add (DataSource.Project, HandleProjectPropertyChanged);

                    if (DataSource.Project.Client != null) {
                        Tracker.Add (DataSource.Project.Client, HandleClientPropertyChanged);
                    }
                }

                if (DataSource.Task != null) {
                    Tracker.Add (DataSource.Task, HandleTaskPropertyChanged);
                }
            }

            Tracker.ClearStale ();
        }

    }
}

