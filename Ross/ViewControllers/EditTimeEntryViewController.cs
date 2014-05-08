using System;
using Cirrious.FluentLayouts.Touch;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;

namespace Toggl.Ross.ViewControllers
{
    public class EditTimeEntryViewController : UIViewController
    {
        private StartStopView startStopView;
        private UIDatePicker datePicker;
        private UIButton projectButton;
        private UITextField descriptionTextField;
        private UIButton tagsButton;
        private UIView billableView;
        private UILabel billableLabel;
        private UISwitch billableSwitch;
        private UIButton deleteButton;

        public EditTimeEntryViewController (TimeEntryModel model)
        {
        }

        public override void LoadView ()
        {
            var scrollView = new UIScrollView ().ApplyStyle (Style.Screen);

            scrollView.Add (startStopView = new StartStopView () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });

            scrollView.AddConstraints (
                startStopView.AtTopOf (scrollView),
                startStopView.WithSameWidth (scrollView),
                startStopView.AtLeftOf (scrollView),
                startStopView.AtRightOf (scrollView),

                startStopView.AtBottomOf (scrollView, 1000f),
                null
            );

            View = scrollView;
        }

        private class StartStopView : UIView
        {
            private readonly UILabel startDateLabel;
            private readonly UILabel startTimeLabel;
            private readonly UIButton startDateTimeButton;
            private readonly UILabel stopDateLabel;
            private readonly UILabel stopTimeLabel;
            private readonly UIButton stopDateTimeButton;
            private readonly UIImageView arrowImageView;
            private LayoutVariant viewLayout = LayoutVariant.StartOnly;
            private bool stopTimeHidden = true;
            private DateTime startTime;
            private DateTime? stopTime;
            private TimeKind selectedTime;

            public StartStopView ()
            {
                startDateTimeButton = new UIButton () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };
                startDateTimeButton.TouchUpInside += OnStartDateTimeButtonTouchUpInside;
                arrowImageView = new UIImageView () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    Image = Image.IconDurationArrow,
                };
                stopDateTimeButton = new UIButton () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };
                stopDateTimeButton.TouchUpInside += OnStopDateTimeButtonTouchUpInside;

                ConstructDateTimeView (startDateTimeButton, ref startDateLabel, ref startTimeLabel);
                ConstructDateTimeView (stopDateTimeButton, ref stopDateLabel, ref stopTimeLabel);

                Add (startDateTimeButton);

                StartTime = DateTime.Now - TimeSpan.FromHours (4);
            }

            private void OnStartDateTimeButtonTouchUpInside (object sender, EventArgs e)
            {
                Selected = Selected == TimeKind.Start ? TimeKind.None : TimeKind.Start;
            }

            private void OnStopDateTimeButtonTouchUpInside (object sender, EventArgs e)
            {
                Selected = Selected == TimeKind.Stop ? TimeKind.None : TimeKind.Stop;
            }

            public DateTime StartTime {
                get { return startTime; }
                set {
                    if (startTime == value)
                        return;

                    startTime = value;
                    var time = startTime.ToLocalTime ();
                    startDateLabel.Text = time.ToLocalizedDateString ();
                    startTimeLabel.Text = time.ToLocalizedTimeString ();
                }
            }

            public DateTime? StopTime {
                get { return stopTime; }
                set {
                    if (stopTime == value)
                        return;

                    stopTime = value;
                    if (stopTime.HasValue) {
                        var time = stopTime.Value.ToLocalTime ();
                        stopDateLabel.Text = time.ToLocalizedDateString ();
                        stopTimeLabel.Text = time.ToLocalizedTimeString ();
                    }
                    SetStopTimeHidden (stopTime == null, animate: Superview != null);
                }
            }

            public TimeKind Selected {
                get { return selectedTime; }
                set {
                    if (selectedTime == value)
                        return;

                    selectedTime = value;

                    if (selectedTime == TimeKind.Start) {
                        startDateLabel.ApplyStyle (Style.EditTimeEntry.DateLabelActive);
                        startTimeLabel.ApplyStyle (Style.EditTimeEntry.TimeLabelActive);
                    } else {
                        startDateLabel.ApplyStyle (Style.EditTimeEntry.DateLabel);
                        startTimeLabel.ApplyStyle (Style.EditTimeEntry.TimeLabel);
                    }

                    if (selectedTime == TimeKind.Stop) {
                        stopDateLabel.ApplyStyle (Style.EditTimeEntry.DateLabelActive);
                        stopTimeLabel.ApplyStyle (Style.EditTimeEntry.TimeLabelActive);
                    } else {
                        stopDateLabel.ApplyStyle (Style.EditTimeEntry.DateLabel);
                        stopTimeLabel.ApplyStyle (Style.EditTimeEntry.TimeLabel);
                    }

                    var handler = SelectedChanged;
                    if (handler != null) {
                        handler (this, EventArgs.Empty);
                    }
                }
            }

            private event EventHandler SelectedChanged;

            private void SetStopTimeHidden (bool hidden, bool animate)
            {
                if (stopTimeHidden == hidden)
                    return;
                stopTimeHidden = hidden;

                if (!animate) {
                    ViewLayout = hidden ? LayoutVariant.StartOnly : LayoutVariant.BothCenterAll;
                } else if (hidden) {
                    ViewLayout = LayoutVariant.BothCenterAll;
                    stopDateTimeButton.Alpha = 1;
                    arrowImageView.Alpha = 1;
                    LayoutIfNeeded ();

                    UIView.AnimateKeyframes (
                        0.4, 0,
                        UIViewKeyframeAnimationOptions.CalculationModeCubic | UIViewKeyframeAnimationOptions.BeginFromCurrentState,
                        delegate {
                            UIView.AddKeyframeWithRelativeStartTime (0, 1, delegate {
                                ViewLayout = LayoutVariant.BothCenterStart;
                                LayoutIfNeeded ();
                            });
                            UIView.AddKeyframeWithRelativeStartTime (0, 0.8, delegate {
                                stopDateTimeButton.Alpha = 0;
                                arrowImageView.Alpha = 0;
                            });
                        },
                        delegate {
                            if (ViewLayout == LayoutVariant.BothCenterStart) {
                                ViewLayout = LayoutVariant.StartOnly;
                                LayoutIfNeeded ();
                            }
                        });
                } else {
                    ViewLayout = LayoutVariant.BothCenterStart;
                    stopDateTimeButton.Alpha = 0;
                    arrowImageView.Alpha = 0;
                    LayoutIfNeeded ();

                    UIView.AnimateKeyframes (
                        0.4, 0,
                        UIViewKeyframeAnimationOptions.CalculationModeCubic | UIViewKeyframeAnimationOptions.BeginFromCurrentState,
                        delegate {
                            UIView.AddKeyframeWithRelativeStartTime (0, 1, delegate {
                                ViewLayout = LayoutVariant.BothCenterAll;
                                LayoutIfNeeded ();
                            });
                            UIView.AddKeyframeWithRelativeStartTime (0.2, 1, delegate {
                                stopDateTimeButton.Alpha = 1;
                                arrowImageView.Alpha = 1;
                            });
                        },
                        delegate {
                        });
                }
            }

            private static void ConstructDateTimeView (UIView view, ref UILabel dateLabel, ref UILabel timeLabel)
            {
                view.Add (dateLabel = new UILabel ().ApplyStyle (Style.EditTimeEntry.DateLabel));
                view.Add (timeLabel = new UILabel ().ApplyStyle (Style.EditTimeEntry.TimeLabel));
                view.AddConstraints (
                    dateLabel.AtTopOf (view, 10f),
                    dateLabel.AtLeftOf (view, 10f),
                    dateLabel.AtRightOf (view, 10f),

                    timeLabel.Below (dateLabel, 2f),
                    timeLabel.AtBottomOf (view, 10f),
                    timeLabel.AtLeftOf (view, 10f),
                    timeLabel.AtRightOf (view, 10f)
                );
                view.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();
            }

            public override void UpdateConstraints ()
            {
                RemoveConstraints (Constraints);

                switch (viewLayout) {
                case LayoutVariant.StartOnly:
                    arrowImageView.RemoveFromSuperview ();
                    stopDateTimeButton.RemoveFromSuperview ();

                    this.AddConstraints (
                        startDateTimeButton.WithSameCenterX (this),
                        startDateTimeButton.WithSameCenterY (this),
                        startDateTimeButton.AtTopOf (this, 10f),
                        startDateTimeButton.AtBottomOf (this, 10f)
                    );
                    break;
                case LayoutVariant.BothCenterStart:
                    AddSubview (arrowImageView);
                    AddSubview (stopDateTimeButton);

                    this.AddConstraints (
                        startDateTimeButton.WithSameCenterX (this),
                        startDateTimeButton.WithSameCenterY (this),
                        startDateTimeButton.AtTopOf (this, 10f),
                        startDateTimeButton.AtBottomOf (this, 10f),

                        arrowImageView.WithSameCenterY (startDateTimeButton),
                        arrowImageView.ToRightOf (startDateTimeButton, 10f),
                        arrowImageView.ToLeftOf (stopDateTimeButton, 10f),

                        stopDateTimeButton.AtTopOf (this, 10f),
                        stopDateTimeButton.AtBottomOf (this, 10f)
                    );
                    break;
                case LayoutVariant.BothCenterAll:
                default:
                    AddSubview (arrowImageView);
                    AddSubview (stopDateTimeButton);

                    this.AddConstraints (
                        startDateTimeButton.AtTopOf (this, 10f),
                        startDateTimeButton.AtBottomOf (this, 10f),

                        arrowImageView.WithSameCenterX (this),
                        arrowImageView.WithSameCenterY (this),
                        arrowImageView.ToRightOf (startDateTimeButton, 10f),
                        arrowImageView.ToLeftOf (stopDateTimeButton, 10f),

                        stopDateTimeButton.AtTopOf (this, 10f),
                        stopDateTimeButton.AtBottomOf (this, 10f)
                    );
                    break;
                }

                base.UpdateConstraints ();
            }

            private LayoutVariant ViewLayout {
                get { return viewLayout; }
                set {
                    if (viewLayout == value)
                        return;
                    viewLayout = value;
                    SetNeedsUpdateConstraints ();
                    SetNeedsLayout ();
                }
            }

            private enum LayoutVariant
            {
                StartOnly,
                BothCenterStart,
                BothCenterAll
            }

            [Export ("requiresConstraintBasedLayout")]
            public static new bool RequiresConstraintBasedLayout ()
            {
                return true;
            }
        }

        private enum TimeKind
        {
            None,
            Start,
            Stop
        }
    }
}
