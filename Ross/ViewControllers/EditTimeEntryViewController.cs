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
            private readonly UIView startDateTimeView;
            private readonly UILabel stopDateLabel;
            private readonly UILabel stopTimeLabel;
            private readonly UIView stopDateTimeView;
            private readonly UIImageView arrowImageView;
            private LayoutVariant viewLayout = LayoutVariant.StartOnly;

            public StartStopView ()
            {
                startDateTimeView = new UIView () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };
                arrowImageView = new UIImageView () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    Image = Image.IconDurationArrow,
                };
                stopDateTimeView = new UIView () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                };

                ConstructDateTimeView (startDateTimeView, ref startDateLabel, ref startTimeLabel);
                ConstructDateTimeView (stopDateTimeView, ref stopDateLabel, ref stopTimeLabel);

                startDateLabel.Text = "Today";
                startTimeLabel.Text = "10:00";
                stopDateLabel.Text = "Today";
                stopTimeLabel.Text = "12:30";

                Add (startDateTimeView);
            }

            public override void TouchesEnded (NSSet touches, UIEvent evt)
            {
                base.TouchesEnded (touches, evt);

                if (ViewLayout == LayoutVariant.StartOnly) {
                    ViewLayout = LayoutVariant.BothCenterStart;
                    stopDateTimeView.Alpha = 0;
                    arrowImageView.Alpha = 0;
                    LayoutIfNeeded ();

                    UIView.AnimateKeyframes (
                        0.5, 0,
                        UIViewKeyframeAnimationOptions.CalculationModeCubic,
                        delegate {
                            UIView.AddKeyframeWithRelativeStartTime (0, 1, delegate {
                                ViewLayout = LayoutVariant.BothCenterAll;
                                LayoutIfNeeded ();
                            });
                            UIView.AddKeyframeWithRelativeStartTime (0.2, 1, delegate {
                                stopDateTimeView.Alpha = 1;
                                arrowImageView.Alpha = 1;
                            });
                        },
                        delegate {
                        });
                } else {
                    ViewLayout = LayoutVariant.BothCenterAll;
                    stopDateTimeView.Alpha = 1;
                    arrowImageView.Alpha = 1;
                    LayoutIfNeeded ();

                    UIView.AnimateKeyframes (
                        0.5, 0,
                        UIViewKeyframeAnimationOptions.CalculationModeCubic,
                        delegate {
                            UIView.AddKeyframeWithRelativeStartTime (0, 1, delegate {
                                ViewLayout = LayoutVariant.BothCenterStart;
                                LayoutIfNeeded ();
                            });
                            UIView.AddKeyframeWithRelativeStartTime (0, 0.8, delegate {
                                stopDateTimeView.Alpha = 0;
                                arrowImageView.Alpha = 0;
                            });
                        },
                        delegate {
                            ViewLayout = LayoutVariant.StartOnly;
                            LayoutIfNeeded ();
                        });
                }
            }

            private static void ConstructDateTimeView (UIView view, ref UILabel dateLabel, ref UILabel timeLabel)
            {
                view.Add (dateLabel = new UILabel ());
                view.Add (timeLabel = new UILabel ());
                view.AddConstraints (
                    dateLabel.AtTopOf (view),
                    dateLabel.AtLeftOf (view),
                    dateLabel.AtRightOf (view),

                    timeLabel.Below (dateLabel, 5f),
                    timeLabel.AtBottomOf (view),
                    timeLabel.AtLeftOf (view),
                    timeLabel.AtRightOf (view)
                );
                view.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints ();
            }

            public override void UpdateConstraints ()
            {
                RemoveConstraints (Constraints);

                switch (viewLayout) {
                case LayoutVariant.StartOnly:
                    arrowImageView.RemoveFromSuperview ();
                    stopDateTimeView.RemoveFromSuperview ();

                    this.AddConstraints (
                        startDateTimeView.WithSameCenterX (this),
                        startDateTimeView.WithSameCenterY (this),
                        startDateTimeView.AtTopOf (this, 20f),
                        startDateTimeView.AtBottomOf (this, 20f)
                    );
                    break;
                case LayoutVariant.BothCenterStart:
                    AddSubview (arrowImageView);
                    AddSubview (stopDateTimeView);

                    this.AddConstraints (
                        startDateTimeView.WithSameCenterX (this),
                        startDateTimeView.WithSameCenterY (this),
                        startDateTimeView.AtTopOf (this, 20f),
                        startDateTimeView.AtBottomOf (this, 20f),

                        arrowImageView.WithSameCenterY (startDateTimeView),
                        arrowImageView.ToRightOf (startDateTimeView, 20f),
                        arrowImageView.ToLeftOf (stopDateTimeView, 20f),

                        stopDateTimeView.AtTopOf (this, 20f),
                        stopDateTimeView.AtBottomOf (this, 20f)
                    );
                    break;
                case LayoutVariant.BothCenterAll:
                default:
                    AddSubview (arrowImageView);
                    AddSubview (stopDateTimeView);

                    this.AddConstraints (
                        startDateTimeView.AtTopOf (this, 20f),
                        startDateTimeView.AtBottomOf (this, 20f),

                        arrowImageView.WithSameCenterX (this),
                        arrowImageView.WithSameCenterY (this),
                        arrowImageView.ToRightOf (startDateTimeView, 20f),
                        arrowImageView.ToLeftOf (stopDateTimeView, 20f),

                        stopDateTimeView.AtTopOf (this, 20f),
                        stopDateTimeView.AtBottomOf (this, 20f)
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
    }
}
