using System;
using System.Collections.Generic;
using System.Drawing;
using Cirrious.FluentLayouts.Touch;
using MonoTouch.CoreAnimation;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Toggl.Phoebe.Data.Models;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using System.Linq;

namespace Toggl.Ross.ViewControllers
{
    public class EditTimeEntryViewController : UIViewController
    {
        private UIView wrapper;
        private StartStopView startStopView;
        private UIDatePicker datePicker;
        private ProjectClientTaskButton projectButton;
        private TextField descriptionTextField;
        private UIButton tagsButton;
        private LabelSwitch billableSwitch;
        private UIButton deleteButton;
        private bool hideDatePicker = true;

        public EditTimeEntryViewController (TimeEntryModel model)
        {
        }

        public override void LoadView ()
        {
            var scrollView = new UIScrollView ().ApplyStyle (Style.Screen);

            scrollView.Add (wrapper = new UIView () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });

            wrapper.Add (startStopView = new StartStopView () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });
            startStopView.SelectedChanged += OnStartStopViewSelectedChanged;

            wrapper.Add (datePicker = new UIDatePicker () {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = hideDatePicker,
                Alpha = 0,
            }.ApplyStyle (Style.EditTimeEntry.DatePicker));

            wrapper.Add (projectButton = new ProjectClientTaskButton () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });

            wrapper.Add (descriptionTextField = new TextField () {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString (
                    "EditEntryDesciptionTimerHint".Tr (),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = (tf) => tf.ResignFirstResponder (),
            }.ApplyStyle (Style.EditTimeEntry.DescriptionField));

            wrapper.Add (tagsButton = new UIButton () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            }.ApplyStyle (Style.EditTimeEntry.TagsButton).ApplyStyle (Style.EditTimeEntry.NoTags));
            tagsButton.SetTitle ("EditEntryTagsHint".Tr (), UIControlState.Normal);

            wrapper.Add (billableSwitch = new LabelSwitch () {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Text = "EditEntryBillable".Tr (),
            }.ApplyStyle (Style.EditTimeEntry.BillableContainer));
            billableSwitch.Label.ApplyStyle (Style.EditTimeEntry.BillableLabel);

            wrapper.Add (deleteButton = new UIButton () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            }.ApplyStyle (Style.EditTimeEntry.DeleteButton));
            deleteButton.SetTitle ("EditEntryDelete".Tr (), UIControlState.Normal);

            wrapper.AddConstraints (VerticalLinearLayout (wrapper));
            scrollView.AddConstraints (
                wrapper.AtTopOf (scrollView),
                wrapper.AtBottomOf (scrollView),
                wrapper.AtLeftOf (scrollView),
                wrapper.AtRightOf (scrollView),
                wrapper.WithSameWidth (scrollView),
                wrapper.Height ().GreaterThanOrEqualTo ().HeightOf (scrollView).Minus (64f),
                null
            );

            View = scrollView;
        }

        private void OnStartStopViewSelectedChanged (object sender, EventArgs e)
        {
            var value = startStopView.Selected == TimeKind.None;

            if (hideDatePicker == value)
                return;

            hideDatePicker = value;

            if (hideDatePicker) {
                UIView.AnimateKeyframes (
                    0.4, 0, 0,
                    delegate {
                        UIView.AddKeyframeWithRelativeStartTime (0, 0.4, delegate {
                            datePicker.Alpha = 0;
                        });
                        UIView.AddKeyframeWithRelativeStartTime (0.2, 0.8, delegate {
                            wrapper.RemoveConstraints (wrapper.Constraints);
                            wrapper.AddConstraints (VerticalLinearLayout (wrapper));
                            View.LayoutIfNeeded ();
                        });
                    },
                    delegate {
                        if (hideDatePicker) {
                            datePicker.Hidden = true;
                        }
                    }
                );
            } else {
                // TODO: Sync date picker value
                datePicker.Hidden = false;

                UIView.AnimateKeyframes (
                    0.4, 0, 0,
                    delegate {
                        UIView.AddKeyframeWithRelativeStartTime (0, 0.6, delegate {
                            wrapper.RemoveConstraints (wrapper.Constraints);
                            wrapper.AddConstraints (VerticalLinearLayout (wrapper));
                            View.LayoutIfNeeded ();
                        });
                        UIView.AddKeyframeWithRelativeStartTime (0.4, 0.8, delegate {
                            datePicker.Alpha = 1;
                        });
                    },
                    delegate {
                    }
                );
            }
        }

        private IEnumerable<FluentLayout> VerticalLinearLayout (UIView container)
        {
            UIView prev = null;

            var subviews = container.Subviews.Where (v => !v.Hidden && !(v == datePicker && hideDatePicker)).ToList ();
            foreach (var v in subviews) {
                var isLast = subviews [subviews.Count - 1] == v;

                if (prev == null) {
                    yield return v.AtTopOf (container);
                } else if (isLast) {
                    yield return v.Top ().GreaterThanOrEqualTo ().BottomOf (prev).Plus (5f);
                } else {
                    yield return v.Below (prev, 5f);
                }
                yield return v.Height ().EqualTo (60f).SetPriority (UILayoutPriority.DefaultLow);
                yield return v.Height ().GreaterThanOrEqualTo (60f);
                yield return v.AtLeftOf (container);
                yield return v.AtRightOf (container);

                prev = v;
            }

            if (prev != null) {
                yield return prev.AtBottomOf (container);
            }
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

            public event EventHandler SelectedChanged;

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

        private class ProjectClientTaskButton : UIButton
        {
            private readonly UIView container;
            private readonly UILabel projectLabel;
            private readonly UILabel clientLabel;
            private readonly UILabel taskLabel;

            public ProjectClientTaskButton ()
            {
                Add (container = new UIView () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                    UserInteractionEnabled = false,
                });

                var maskLayer = new CAGradientLayer () {
                    AnchorPoint = PointF.Empty,
                    StartPoint = new PointF (0.0f, 0.0f),
                    EndPoint = new PointF (1.0f, 0.0f),
                    Colors = new [] {
                        UIColor.FromWhiteAlpha (1, 1).CGColor,
                        UIColor.FromWhiteAlpha (1, 1).CGColor,
                        UIColor.FromWhiteAlpha (1, 0).CGColor,
                    },
                    Locations = new [] {
                        NSNumber.FromFloat (0f),
                        NSNumber.FromFloat (0.9f),
                        NSNumber.FromFloat (1f),
                    },
                };
                container.Layer.Mask = maskLayer;

                container.Add (projectLabel = new UILabel () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                }.ApplyStyle (Style.EditTimeEntry.ProjectLabel));
                container.Add (clientLabel = new UILabel () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                }.ApplyStyle (Style.EditTimeEntry.ClientLabel));
                container.Add (taskLabel = new UILabel () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                }.ApplyStyle (Style.EditTimeEntry.TaskLabel));
            }

            public override void UpdateConstraints ()
            {
                RemoveConstraints (Constraints);

                this.AddConstraints (
                    container.AtLeftOf (this, 15f),
                    container.WithSameCenterY (this),

                    projectLabel.AtTopOf (container),
                    projectLabel.AtLeftOf (container),
                    null
                );

                if (!clientLabel.Hidden) {
                    var baselineOffset = (float)Math.Floor (projectLabel.Font.Descender - clientLabel.Font.Descender);
                    this.AddConstraints (
                        clientLabel.AtTopOf (container, -baselineOffset),
                        clientLabel.AtRightOf (container),
                        clientLabel.ToRightOf (projectLabel, 5f),
                        clientLabel.AtBottomOf (projectLabel, baselineOffset),
                        null
                    );
                } else {
                    this.AddConstraints (
                        projectLabel.AtRightOf (container),
                        null
                    );
                }

                if (!taskLabel.Hidden) {
                    this.AddConstraints (
                        taskLabel.Below (projectLabel, 3f),
                        taskLabel.AtLeftOf (container),
                        taskLabel.AtRightOf (container),
                        taskLabel.AtBottomOf (container),
                        null
                    );
                } else {
                    this.AddConstraints (
                        projectLabel.AtBottomOf (container),
                        null
                    );
                }

                base.UpdateConstraints ();
            }

            public override void LayoutSubviews ()
            {
                base.LayoutSubviews ();

                // Update fade mask position:
                var bounds = container.Frame;
                bounds.Width = Frame.Width - bounds.X;
                container.Layer.Mask.Bounds = bounds;
            }

            public string ProjectName {
                get { return projectLabel.Text; }
                set { projectLabel.Text = value; }
            }

            public string ClientName {
                get { return clientLabel.Text; }
                set {
                    if (clientLabel.Text == value)
                        return;

                    var visibilityChanged = String.IsNullOrWhiteSpace (clientLabel.Text) != String.IsNullOrWhiteSpace (value);
                    clientLabel.Text = value;

                    if (visibilityChanged) {
                        SetNeedsUpdateConstraints ();
                        clientLabel.Hidden = String.IsNullOrWhiteSpace (value);
                    }
                }
            }

            public string TaskName {
                get { return taskLabel.Text; }
                set {
                    if (taskLabel.Text == value)
                        return;

                    var visibilityChanged = String.IsNullOrWhiteSpace (taskLabel.Text) != String.IsNullOrWhiteSpace (value);
                    taskLabel.Text = value;

                    if (visibilityChanged) {
                        SetNeedsUpdateConstraints ();
                        taskLabel.Hidden = String.IsNullOrWhiteSpace (value);
                    }
                }
            }

            public UIColor ProjectColor {
                set { SetBackgroundImage (value.ToImage (), UIControlState.Normal); }
            }

            [Export ("requiresConstraintBasedLayout")]
            public static new bool RequiresConstraintBasedLayout ()
            {
                return true;
            }
        }

        private class LabelSwitch : UIView
        {
            private readonly UILabel label;
            private readonly UISwitch toggle;

            public LabelSwitch ()
            {
                Add (label = new UILabel () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                });
                Add (toggle = new UISwitch () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                });
            }

            public override void UpdateConstraints ()
            {
                if (Constraints.Length == 0) {
                    this.AddConstraints (
                        toggle.AtRightOf (this, 15f),
                        toggle.WithSameCenterY (this),

                        label.AtLeftOf (this, 15f),
                        label.WithSameCenterY (this),
                        label.ToLeftOf (toggle, 5f),

                        null
                    );
                }

                base.UpdateConstraints ();
            }

            public string Text {
                get { return label.Text; }
                set { label.Text = value; }
            }

            public UILabel Label {
                get { return label; }
            }

            public UISwitch Switch {
                get { return toggle; }
            }

            [Export ("requiresConstraintBasedLayout")]
            public static new bool RequiresConstraintBasedLayout ()
            {
                return true;
            }
        }
    }
}
