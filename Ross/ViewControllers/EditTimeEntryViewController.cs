using System;
using System.Collections.Generic;
using System.Linq;
using Cirrious.FluentLayouts.Touch;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using GalaSoft.MvvmLight.Helpers;
using Toggl.Phoebe;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.Data.Utils;
using Toggl.Phoebe.Data.ViewModels;
using Toggl.Phoebe.Data.Views;
using Toggl.Ross.DataSources;
using Toggl.Ross.Theme;
using Toggl.Ross.Views;
using UIKit;
using XPlatUtils;

namespace Toggl.Ross.ViewControllers
{
    public class EditTimeEntryViewController : UIViewController
    {
        private enum LayoutVariant {
            Default,
            Description
        }

        class TGTableView : UITableView
        {
            public override UIView TableFooterView
            {
                get {
                    return base.TableFooterView;
                } set {
                    base.TableFooterView = value ?? new UIView ();
                }
            }
        }

        private LayoutVariant layoutVariant = LayoutVariant.Default;
        private readonly TimerNavigationController timerController;
        private NSLayoutConstraint[] trackedWrapperConstraints;
        private UIView wrapper;
        private StartStopView startStopView { get; set; }
        private ProjectClientTaskButton projectButton { get; set; }
        private TextField descriptionTextField { get; set; }
        private LabelSwitchView billableSwitch { get; set; }
        private UIDatePicker datePicker;
        private UIButton tagsButton;
        private UIButton deleteButton;
        private bool hideDatePicker = true;
        private readonly List<NSObject> notificationObjects = new List<NSObject> ();
        private UITableView autoCompletionTableView;
        private UIBarButtonItem autoCompletionDoneBarButtonItem;
        private Stack<UIBarButtonItem> barButtonItemsStack = new Stack<UIBarButtonItem> ();

        // to avoid weak references to be removed
        private Binding<string, string> durationBinding, projectBinding, clientBinding, descriptionBinding, taskBinding, projectColorBinding;
        private Binding<DateTime, DateTime> startTimeBinding;
        private Binding<DateTime, DateTime?> stopTimeBinding;
        private Binding<List<TagData>, List<TagData>> tagBinding;
        private Binding<bool, bool> isBillableBinding, billableBinding, isRunningBinding, isPremiumBinding;

        private readonly TimeEntryData data;
        protected EditTimeEntryViewModel ViewModel { get; set; }

        public EditTimeEntryViewController (TimeEntryData data)
        {
            this.data = data;
        }

        protected override void Dispose (bool disposing)
        {
            base.Dispose (disposing);
        }

        private void ResetWrapperConstraints()
        {
            if (trackedWrapperConstraints != null) {
                wrapper.RemoveConstraints (trackedWrapperConstraints);
                trackedWrapperConstraints = null;
            }

            switch (layoutVariant) {
            case LayoutVariant.Default:
                trackedWrapperConstraints = VerticalLinearLayout (wrapper).ToLayoutConstraints ();
                break;
            case LayoutVariant.Description:
                trackedWrapperConstraints = new [] {
                    descriptionTextField.AtTopOf (wrapper),
                    descriptionTextField.AtLeftOf (wrapper),
                    descriptionTextField.AtRightOf (wrapper),
                    descriptionTextField.Height ().EqualTo (60.0f),
                    autoCompletionTableView.AtTopOf (wrapper, 65.0f),
                    autoCompletionTableView.AtLeftOf (wrapper),
                    autoCompletionTableView.AtRightOf (wrapper),
                    autoCompletionTableView.AtBottomOf (wrapper)
                } .ToLayoutConstraints ();
                break;
            }
            wrapper.AddConstraints (trackedWrapperConstraints);
        }

        public override void LoadView ()
        {
            var scrollView = new UIScrollView ().Apply (Style.Screen);

            scrollView.Add (wrapper = new UIView () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });

            wrapper.Add (startStopView = new StartStopView {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });
            startStopView.SelectedChanged += OnStartStopViewSelectedChanged;

            wrapper.Add (datePicker = new UIDatePicker {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = DatePickerHidden,
                Alpha = 0,
            } .Apply (Style.EditTimeEntry.DatePicker));
            datePicker.ValueChanged += OnDatePickerValueChanged;

            wrapper.Add (projectButton = new ProjectClientTaskButton {
                TranslatesAutoresizingMaskIntoConstraints = false,
            });
            projectButton.TouchUpInside += OnProjectButtonTouchUpInside;

            wrapper.Add (descriptionTextField = new TextField {
                TranslatesAutoresizingMaskIntoConstraints = false,
                AttributedPlaceholder = new NSAttributedString (
                    "EditEntryDesciptionTimerHint".Tr (),
                    foregroundColor: Color.Gray
                ),
                ShouldReturn = tf => tf.ResignFirstResponder (),
            } .Apply (Style.EditTimeEntry.DescriptionField));

            descriptionTextField.ShouldBeginEditing += (s) => {
                ForceDimissDatePicker();
                return true;
            };
            descriptionTextField.ShouldEndEditing += s => {
                return true;
            };

            wrapper.Add (tagsButton = new UIButton () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            } .Apply (Style.EditTimeEntry.TagsButton));
            tagsButton.TouchUpInside += OnTagsButtonTouchUpInside;

            wrapper.Add (billableSwitch = new LabelSwitchView () {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Text = "EditEntryBillable".Tr (),
            } .Apply (Style.EditTimeEntry.BillableContainer));
            billableSwitch.Label.Apply (Style.EditTimeEntry.BillableLabel);

            wrapper.Add (autoCompletionTableView = new TGTableView() {
                TranslatesAutoresizingMaskIntoConstraints = false,
                EstimatedRowHeight = 60.0f,
                BackgroundColor = UIColor.Clear
            });

            wrapper.Add (deleteButton = new UIButton () {
                TranslatesAutoresizingMaskIntoConstraints = false,
            } .Apply (Style.EditTimeEntry.DeleteButton));
            deleteButton.SetTitle ("EditEntryDelete".Tr (), UIControlState.Normal);
            deleteButton.TouchUpInside += OnDeleteButtonTouchUpInside;

            ResetWrapperConstraints ();
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

        public async override void ViewDidLoad ()
        {
            base.ViewDidLoad ();
            //timerController.Attach (this);

            ViewModel = await EditTimeEntryViewModel.Init (data);

            // Bindings.
            startTimeBinding = this.SetBinding (() => ViewModel.StartDate, () => startStopView.StartTime);
            stopTimeBinding = this.SetBinding (() => ViewModel.StopDate, () => startStopView.StopTime);
            projectBinding = this.SetBinding (() => ViewModel.ProjectName, () => projectButton.ProjectName)
                             .ConvertSourceToTarget (name => string.IsNullOrEmpty (name) ? "EditEntryProjectHint".Tr () : name);
            projectColorBinding = this.SetBinding (() => ViewModel.ProjectColorHex, () => projectButton.ProjectColorHex);
            taskBinding = this.SetBinding (() => ViewModel.TaskName, () => projectButton.TaskName);
            clientBinding = this.SetBinding (() => ViewModel.ClientName, () => projectButton.ClientName);
            tagBinding = this.SetBinding (() => ViewModel.TagList).WhenSourceChanges (() => {
                FeedTags (ViewModel.TagList.Select (tag => tag.Name).ToList (), tagsButton);
            });
            descriptionBinding = this.SetBinding (() => ViewModel.Description, () => descriptionTextField.Text);
            isPremiumBinding = this.SetBinding (() => ViewModel.IsPremium, () => billableSwitch.Hidden).ConvertSourceToTarget (isPremium => !isPremium);
            isRunningBinding = this.SetBinding (() => ViewModel.IsRunning).WhenSourceChanges (() => {
                if (ViewModel.IsRunning) {
                    startStopView.StopTime = null;
                } else {
                    startStopView.StopTime = ViewModel.StopDate;
                }
            });
            isBillableBinding = this.SetBinding (() => ViewModel.IsBillable, () => billableSwitch.Switch.On);
        }

        private void SetEditingModeViewsHidden (bool editingMode)
        {
            billableSwitch.Alpha = tagsButton.Alpha = startStopView.Alpha = projectButton.Alpha = deleteButton.Alpha = editingMode ? 0 : 1;
            autoCompletionTableView.Alpha = 1 - tagsButton.Alpha;
        }

        private void OnDatePickerValueChanged (object sender, EventArgs e)
        {
            switch (startStopView.Selected) {
            case TimeKind.Start:
                //model.StartTime = datePicker.Date.ToDateTime ();
                break;
            case TimeKind.Stop:
                //model.StopTime = datePicker.Date.ToDateTime ();
                break;
            }
        }

        private void FeedTags (List<string> tagNames, UIButton btn)
        {
            // Construct tags attributed strings:
            NSMutableAttributedString text = null;
            foreach (var tag in tagNames) {

                var chip = NSAttributedString.CreateFrom (new NSTextAttachment {
                    Image = ServiceContainer.Resolve<TagChipCache> ().Get (tag, btn),
                });

                if (text == null) {
                    text = new NSMutableAttributedString (chip);
                } else {
                    text.Append (new NSAttributedString (" ", Style.EditTimeEntry.WithTags));
                    text.Append (chip);
                }
            }

            if (text == null) {
                btn.SetAttributedTitle (new NSAttributedString ("EditEntryTagsHint".Tr (), Style.EditTimeEntry.NoTags), UIControlState.Normal);
            } else {
                btn.SetAttributedTitle (text, UIControlState.Normal);
            }
        }

        private void OnProjectButtonTouchUpInside (object sender, EventArgs e)
        {
            //var controller = new ProjectSelectionViewController (model);
            //NavigationController.PushViewController (controller, true);
        }

        bool shouldUpdateAutocompletionTableViewSource = false;
        NSTimer autocompletionModeTimeoutTimer;

        private void OnTagsButtonTouchUpInside (object sender, EventArgs e)
        {
            //var controller = new TagSelectionViewController (model);
            //NavigationController.PushViewController (controller, true);
        }

        private void OnDeleteButtonTouchUpInside (object sender, EventArgs e)
        {
            var alert = new UIAlertView (
                "EditEntryConfirmTitle".Tr (),
                "EditEntryConfirmMessage".Tr (),
                null,
                "EditEntryConfirmCancel".Tr (),
                "EditEntryConfirmDelete".Tr ());
            alert.Clicked += async (s, ev) => {
                if (ev.ButtonIndex == 1) {
                    NavigationController.PopToRootViewController (true);
                }
            };
            alert.Show ();
        }

        public override void ViewWillAppear (bool animated)
        {
            base.ViewWillAppear (animated);

            //timerController.Start ();

            ObserveNotification (UIKeyboard.WillHideNotification, (notif) => {
                OnKeyboardHeightChanged (0);
            });
            ObserveNotification (UIKeyboard.WillShowNotification, (notif) => {
                var val = notif.UserInfo.ObjectForKey (UIKeyboard.FrameEndUserInfoKey) as NSValue;
                if (val != null) {
                    OnKeyboardHeightChanged ((int)val.CGRectValue.Height);
                }
            });
            ObserveNotification (UIKeyboard.WillChangeFrameNotification, (notif) => {
                var val = notif.UserInfo.ObjectForKey (UIKeyboard.FrameEndUserInfoKey) as NSValue;
                if (val != null) {
                    OnKeyboardHeightChanged ((int)val.CGRectValue.Height);
                }
            });
        }

        private void ObserveNotification (string name, Action<NSNotification> callback)
        {
            var obj = NSNotificationCenter.DefaultCenter.AddObserver (new NSString ( name), callback);
            if (obj != null) {
                notificationObjects.Add (obj);
            }
        }

        public override void ViewDidAppear (bool animated)
        {
            base.ViewDidAppear (animated);

            ServiceContainer.Resolve<ITracker> ().CurrentScreen = "Edit Time Entry";
        }

        public override void ViewWillDisappear (bool animated)
        {
            base.ViewWillDisappear (animated);
            NSNotificationCenter.DefaultCenter.RemoveObservers (notificationObjects);
            notificationObjects.Clear ();
        }

        public override void ViewDidDisappear (bool animated)
        {
            base.ViewDidDisappear (animated);
            //timerController.Stop ();
        }

        private void OnKeyboardHeightChanged (int height)
        {
            var scrollView = (UIScrollView)View;

            var inset = scrollView.ContentInset;
            inset.Bottom = height;
            scrollView.ContentInset = inset;

            inset = scrollView.ScrollIndicatorInsets;
            inset.Bottom = height;
            scrollView.ScrollIndicatorInsets = inset;
        }

        private void OnStartStopViewSelectedChanged (object sender, EventArgs e)
        {
            DatePickerHidden = startStopView.Selected == TimeKind.None;
        }

        private void ForceDimissDatePicker()
        {
            DatePickerHidden = true;
            startStopView.Selected = TimeKind.None;
        }

        private bool DatePickerHidden
        {
            get { return hideDatePicker; }
            set {
                if (hideDatePicker == value) {
                    return;
                }
                hideDatePicker = value;

                if (hideDatePicker) {
                    UIView.AnimateKeyframes (
                        0.4, 0, 0,
                    delegate {
                        UIView.AddKeyframeWithRelativeStartTime (0, 0.4, delegate {
                            datePicker.Alpha = 0;
                        });
                        UIView.AddKeyframeWithRelativeStartTime (0.2, 0.8, delegate {
                            ResetWrapperConstraints();
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
                    descriptionTextField.ResignFirstResponder ();
                    datePicker.Hidden = false;
                    UIView.AnimateKeyframes (
                        0.4, 0, 0,
                    delegate {
                        UIView.AddKeyframeWithRelativeStartTime (0, 0.6, delegate {
                            ResetWrapperConstraints();
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
        }

        private IEnumerable<FluentLayout> VerticalLinearLayout (UIView container)
        {
            UIView prev = null;

            var subviews = container.Subviews.Where (v => !v.Hidden && ! (v == datePicker && DatePickerHidden)).ToList ();
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
            private readonly List<NSLayoutConstraint> trackedConstraints = new List<NSLayoutConstraint>();
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

            public DateTime StartTime
            {
                get { return startTime; }
                set {
                    if (startTime == value) {
                        return;
                    }

                    startTime = value;
                    var time = startTime.ToLocalTime ();
                    startDateLabel.Text = time.ToLocalizedDateString ();
                    startTimeLabel.Text = time.ToLocalizedTimeString ();
                }
            }

            public DateTime? StopTime
            {
                get { return stopTime; }
                set {
                    if (stopTime == value) {
                        return;
                    }

                    stopTime = value;
                    if (stopTime.HasValue) {
                        var time = stopTime.Value.ToLocalTime ();
                        stopDateLabel.Text = time.ToLocalizedDateString ();
                        stopTimeLabel.Text = time.ToLocalizedTimeString ();
                    }
                    SetStopTimeHidden (stopTime == null, animate: Superview != null);

                    if (stopTime == null && Selected == TimeKind.Stop) {
                        Selected = TimeKind.None;
                    }
                }
            }

            public TimeKind Selected
            {
                get { return selectedTime; }
                set {
                    if (selectedTime == value) {
                        return;
                    }

                    selectedTime = value;

                    if (selectedTime == TimeKind.Start) {
                        startDateLabel.Apply (Style.EditTimeEntry.DateLabelActive);
                        startTimeLabel.Apply (Style.EditTimeEntry.TimeLabelActive);
                    } else {
                        startDateLabel.Apply (Style.EditTimeEntry.DateLabel);
                        startTimeLabel.Apply (Style.EditTimeEntry.TimeLabel);
                    }

                    if (selectedTime == TimeKind.Stop) {
                        stopDateLabel.Apply (Style.EditTimeEntry.DateLabelActive);
                        stopTimeLabel.Apply (Style.EditTimeEntry.TimeLabelActive);
                    } else {
                        stopDateLabel.Apply (Style.EditTimeEntry.DateLabel);
                        stopTimeLabel.Apply (Style.EditTimeEntry.TimeLabel);
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
                if (stopTimeHidden == hidden) {
                    return;
                }
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
                view.Add (dateLabel = new UILabel ().Apply (Style.EditTimeEntry.DateLabel));
                view.Add (timeLabel = new UILabel ().Apply (Style.EditTimeEntry.TimeLabel));
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
                if (trackedConstraints.Count > 0) {
                    RemoveConstraints (trackedConstraints.ToArray());
                    trackedConstraints.Clear ();
                }

                switch (viewLayout) {
                case LayoutVariant.StartOnly:
                    arrowImageView.RemoveFromSuperview ();
                    stopDateTimeButton.RemoveFromSuperview ();

                    trackedConstraints.AddRange (new [] {
                        startDateTimeButton.WithSameCenterX (this),
                        startDateTimeButton.WithSameCenterY (this),
                        startDateTimeButton.AtTopOf (this, 10f),
                        startDateTimeButton.AtBottomOf (this, 10f)
                    } .ToLayoutConstraints());
                    break;
                case LayoutVariant.BothCenterStart:
                    AddSubview (arrowImageView);
                    AddSubview (stopDateTimeButton);

                    trackedConstraints.AddRange (new [] {
                        startDateTimeButton.WithSameCenterX (this),
                        startDateTimeButton.WithSameCenterY (this),
                        startDateTimeButton.AtTopOf (this, 10f),
                        startDateTimeButton.AtBottomOf (this, 10f),

                        arrowImageView.WithSameCenterY (startDateTimeButton),
                        arrowImageView.ToRightOf (startDateTimeButton, 10f),
                        arrowImageView.ToLeftOf (stopDateTimeButton, 10f),

                        stopDateTimeButton.AtTopOf (this, 10f),
                        stopDateTimeButton.AtBottomOf (this, 10f)
                    } .ToLayoutConstraints());
                    break;
                default:
                    AddSubview (arrowImageView);
                    AddSubview (stopDateTimeButton);

                    trackedConstraints.AddRange (new [] {
                        startDateTimeButton.AtTopOf (this, 10f),
                        startDateTimeButton.AtBottomOf (this, 10f),

                        arrowImageView.WithSameCenterX (this),
                        arrowImageView.WithSameCenterY (this),
                        arrowImageView.ToRightOf (startDateTimeButton, 10f),
                        arrowImageView.ToLeftOf (stopDateTimeButton, 10f),

                        stopDateTimeButton.AtTopOf (this, 10f),
                        stopDateTimeButton.AtBottomOf (this, 10f)
                    } .ToLayoutConstraints());
                    break;
                }

                AddConstraints (trackedConstraints.ToArray ());

                base.UpdateConstraints ();
            }

            private LayoutVariant ViewLayout
            {
                get { return viewLayout; }
                set {
                    if (viewLayout == value) {
                        return;
                    }
                    viewLayout = value;
                    SetNeedsUpdateConstraints ();
                    SetNeedsLayout ();
                }
            }

            private enum LayoutVariant {
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

        private enum TimeKind {
            None,
            Start,
            Stop
        }

        private class ProjectClientTaskButton : UIButton
        {
            private readonly List<NSLayoutConstraint> trackedConstraints = new List<NSLayoutConstraint>();
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
                    AnchorPoint = CGPoint.Empty,
                    StartPoint = new CGPoint (0.0f, 0.0f),
                    EndPoint = new CGPoint (1.0f, 0.0f),
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
                } .Apply (Style.EditTimeEntry.ProjectLabel));
                container.Add (clientLabel = new UILabel () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                } .Apply (Style.EditTimeEntry.ClientLabel));
                container.Add (taskLabel = new UILabel () {
                    TranslatesAutoresizingMaskIntoConstraints = false,
                } .Apply (Style.EditTimeEntry.TaskLabel));
            }

            public override void UpdateConstraints ()
            {
                if (trackedConstraints.Count > 0) {
                    RemoveConstraints (trackedConstraints.ToArray());
                    trackedConstraints.Clear ();
                }

                trackedConstraints.AddRange (new FluentLayout[] {
                    container.AtLeftOf (this, 15f),
                    container.WithSameCenterY (this),

                    projectLabel.AtTopOf (container),
                    projectLabel.AtLeftOf (container),
                    null
                } .ToLayoutConstraints());

                if (!clientLabel.Hidden) {
                    var baselineOffset = (float)Math.Floor (projectLabel.Font.Descender - clientLabel.Font.Descender);
                    trackedConstraints.AddRange (new FluentLayout[] {
                        clientLabel.AtTopOf (container, -baselineOffset),
                        clientLabel.AtRightOf (container),
                        clientLabel.ToRightOf (projectLabel, 5f),
                        clientLabel.AtBottomOf (projectLabel, baselineOffset),
                        null
                    } .ToLayoutConstraints());
                } else {
                    trackedConstraints.AddRange (new FluentLayout[] {
                        projectLabel.AtRightOf (container),
                        null
                    } .ToLayoutConstraints());
                }

                if (!taskLabel.Hidden) {
                    trackedConstraints.AddRange (new FluentLayout[] {
                        taskLabel.Below (projectLabel, 3f),
                        taskLabel.AtLeftOf (container),
                        taskLabel.AtRightOf (container),
                        taskLabel.AtBottomOf (container),
                        null
                    } .ToLayoutConstraints());
                } else {
                    trackedConstraints.AddRange (new FluentLayout[] {
                        projectLabel.AtBottomOf (container),
                        null
                    } .ToLayoutConstraints());
                }

                AddConstraints (trackedConstraints.ToArray ());

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

            public string ProjectName
            {
                get { return projectLabel.Text; }
                set { projectLabel.Text = value; }
            }

            public string ClientName
            {
                get { return clientLabel.Text; }
                set {
                    if (clientLabel.Text == value) {
                        return;
                    }

                    var visibilityChanged = String.IsNullOrWhiteSpace (clientLabel.Text) != String.IsNullOrWhiteSpace (value);
                    clientLabel.Text = value;

                    if (visibilityChanged) {
                        SetNeedsUpdateConstraints ();
                        clientLabel.Hidden = String.IsNullOrWhiteSpace (value);
                    }
                }
            }

            public string TaskName
            {
                get { return taskLabel.Text; }
                set {
                    if (taskLabel.Text == value) {
                        return;
                    }

                    var visibilityChanged = string.IsNullOrWhiteSpace (taskLabel.Text) != String.IsNullOrWhiteSpace (value);
                    taskLabel.Text = value;

                    if (visibilityChanged) {
                        SetNeedsUpdateConstraints ();
                        taskLabel.Hidden = String.IsNullOrWhiteSpace (value);
                    }
                }
            }

            public string ProjectColorHex
            {
                get { return "#ffffff"; }
                set {
                    var colorHex = (value == "#4dc3ff" || string.IsNullOrEmpty (value)) ? "#ffffff" : value;
                    var color = UIColor.Clear.FromHex (colorHex);

                    if (color == Color.White) {
                        projectLabel.Apply (Style.EditTimeEntry.ProjectHintLabel);
                        SetBackgroundImage (Color.White.ToImage (), UIControlState.Normal);
                        SetBackgroundImage (Color.LightestGray.ToImage (), UIControlState.Highlighted);
                    } else {
                        projectLabel.Apply (Style.EditTimeEntry.ProjectLabel);
                        SetBackgroundImage (color.ToImage (), UIControlState.Normal);
                        SetBackgroundImage (null, UIControlState.Highlighted);
                    }
                }
            }

            [Export ("requiresConstraintBasedLayout")]
            public static new bool RequiresConstraintBasedLayout ()
            {
                return true;
            }
        }
    }
}
