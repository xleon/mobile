using System;
using Cirrious.FluentLayouts.Touch;
using Toggl.Ross.Theme;
using UIKit;

namespace Toggl.Ross.Views
{
    public class TimerBar : UIToolbar
    {
        public enum State
        {
            TimerInactive,
            TimerRunning,
            ManualMode
        }

        UIButton startButton;
        UIView startButtonCircle;
        UISwitch manualSwitch;

        UILabel timerLabel;
        UILabel manualLabel;

        UILabel durationLabel;

        public event EventHandler StartButtonHit;
        public event EventHandler ManualModeSwitchHit;

        public bool IsManualModeSwitchOn => this.manualSwitch.On;

        public TimerBar()
        {
            this.Add(this.startButtonCircle = new UIView().Apply(Style.Timer.StartButtonCircle));

            this.Add(this.startButton = UIButton.FromType(UIButtonType.Custom));

            this.Add(this.manualSwitch = new UISwitch().Apply(Style.Timer.TimerModeSwitch));

            this.Add(this.timerLabel = new UILabel { Text = "Timer" }
            .Apply(Style.Timer.TimerModeSwitchLabel));
            this.Add(this.manualLabel = new UILabel { Text = "Manual" }
            .Apply(Style.Timer.TimerModeSwitchLabel));

            this.Add(this.durationLabel = new UILabel().Apply(Style.Timer.TimerDurationLabel));

            this.SubviewsDoNotTranslateAutoresizingMaskIntoConstraints();

            this.AddConstraints(
                this.startButton.Width().EqualTo(92),
                this.startButton.AtRightOf(this),
                this.startButton.AtTopOf(this),
                this.startButton.AtBottomOf(this),

                this.startButtonCircle.WithSameCenterX(this.startButton),
                this.startButtonCircle.WithSameCenterY(this.startButton),
                this.startButtonCircle.Width().EqualTo(40),
                this.startButtonCircle.Height().EqualTo(40),

                this.timerLabel.AtLeftOf(this, 16),
                this.manualSwitch.ToRightOf(this.timerLabel, 0),
                this.manualLabel.ToRightOf(this.manualSwitch, 0),

                this.timerLabel.WithSameCenterY(this),
                this.manualLabel.WithSameCenterY(this),
                this.manualSwitch.WithSameCenterY(this),

                this.durationLabel.WithSameCenterY(this),
                this.durationLabel.AtRightOf(this, 265)

            );

            this.manualSwitch.ValueChanged += this.onManualSwitchValueChanged;
            this.startButton.TouchUpInside += this.onStartButtonTouchUpInside;

            this.SetState(State.TimerRunning);
        }

        private void onStartButtonTouchUpInside(object sender, EventArgs e)
        {
            this.StartButtonHit?.Invoke(this, EventArgs.Empty);
        }

        private void onManualSwitchValueChanged(object sender, EventArgs e)
        {
            this.SetState(this.manualSwitch.On ? State.ManualMode : State.TimerInactive);
            this.ManualModeSwitchHit?.Invoke(this, EventArgs.Empty);
        }

        public void SetDurationText(string text)
        {
            this.durationLabel.Text = text;
        }

        public void SetState(State state)
        {
            var isRunning = state == State.TimerRunning;
            var isManual = state == State.ManualMode;

            this.manualLabel.Hidden = isRunning;
            this.timerLabel.Hidden = isRunning;
            this.manualSwitch.Hidden = isRunning;
            this.durationLabel.Hidden = !isRunning;

            this.manualSwitch.On = isManual;

            switch (state)
            {
                case State.TimerInactive:
                {
                    this.manualLabel.TextColor = Color.TextInactive;
                    this.timerLabel.TextColor = Color.StartButton;

                    this.startButtonCircle.BackgroundColor = Color.StartButton;
                    break;
                }
                case State.TimerRunning:
                {
                    this.startButtonCircle.BackgroundColor = Color.StopButton;
                    break;
                }
                case State.ManualMode:
                {
                    this.manualLabel.TextColor = Color.AddManualButton;
                    this.timerLabel.TextColor = Color.TextInactive;

                    this.startButtonCircle.BackgroundColor = Color.AddManualButton;
                    break;
                }
            }
        }
    }
}

