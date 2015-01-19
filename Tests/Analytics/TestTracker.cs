using System;
using Toggl.Phoebe.Analytics;

namespace Toggl.Phoebe.Tests.Analytics
{
    public class TestTracker : BaseTracker
    {

        public const string SendEventExceptionMessage = "SendEventCalled";
        public const string SendTimingExceptionMessage = "SendTimingCalled";
        public const string StartNewSessionException = "StartNewSession";

        public SendData CurrentSendData { get; set; }

        protected override void StartNewSession()
        {
            throw new ArgumentException (StartNewSessionException);
        }

        protected override void SendTiming (long elapsedMilliseconds, string category, string variable, string label = null)
        {
            throw new ArgumentException (SendTimingExceptionMessage);
        }

        protected override void SendEvent (string category, string action, string label = null, long value = 0L)
        {
            CurrentSendData = new SendData ();
            CurrentSendData.Category = category;
            CurrentSendData.Action = action;
            CurrentSendData.Label = label;
            throw new ArgumentException (SendEventExceptionMessage);
        }

        protected override void SetCustomDimension (int idx, string value)
        {
        }

        public override string CurrentScreen
        {
            set {
                throw new ArgumentException (StartNewSessionException);
            }
        }

        public class SendData
        {
            public string Category;
            public string Action;
            public string Label;
        }
    }
}

