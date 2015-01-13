using System;
using Moq;
using NUnit.Framework;
using Toggl.Phoebe.Analytics;
using Toggl.Phoebe.Data;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Analytics
{
    [TestFixture]
    public class BaseTrackerTest : Test
    {
        private TestTracker tracker;

        public override void SetUp ()
        {
            base.SetUp();
            ServiceContainer.Register<ISettingsStore> (Mock.Of<ISettingsStore> (
                        (store) => store.ExperimentId == (string)null));
            ServiceContainer.Register<ExperimentManager> (new ExperimentManager ());
            tracker = new TestTracker ();
        }

        [Test]
        public void TestSendSettingsChangeEvent ()
        {
            try {
                tracker.SendSettingsChangeEvent (SettingName.AskForProject);
            } catch (Exception e) {
                Assert.AreEqual (TestTracker.SendEventExceptionMessage, e.Message);
                Assert.AreEqual (tracker.CurrentSendData.Label, "AskForProject");
            }

            try {
                tracker.SendSettingsChangeEvent ((SettingName)100);
            } catch (Exception e) {
                Assert.AreNotEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }
        }

        [Test]
        public void TestSendAccountLoginEvent ()
        {
            try {
                tracker.SendAccountLoginEvent (AccountCredentials.Password);
            } catch (Exception e) {
                Assert.AreEqual (TestTracker.SendEventExceptionMessage, e.Message);
                Assert.AreEqual (tracker.CurrentSendData.Label, "Password");
            }

            try {
                tracker.SendAccountLoginEvent ((AccountCredentials)100);
            } catch (Exception e) {
                Assert.AreNotEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }
        }

        [Test]
        public void TestSendAccountCreateEvent ()
        {
            try {
                tracker.SendAccountCreateEvent (AccountCredentials.Password);
            } catch (Exception e) {
                Assert.AreEqual (TestTracker.SendEventExceptionMessage, e.Message);
                Assert.AreEqual (tracker.CurrentSendData.Label, "Password");
            }

            try {
                tracker.SendAccountCreateEvent ((AccountCredentials)100);
            } catch (Exception e) {
                Assert.AreNotEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }
        }

        [Test]
        public void TestSendAccountLogoutEvent ()
        {
            try {
                tracker.SendAccountLogoutEvent ();
            } catch (Exception e) {
                Assert.AreEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }
        }

        [Test]
        public void TestSendTimerStartEvent ()
        {
            try {
                tracker.SendTimerStartEvent (TimerStartSource.AppNew);
            } catch (Exception e) {
                Assert.AreEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }

            try {
                tracker.SendTimerStartEvent ((TimerStartSource)100);
            } catch (Exception e) {
                Assert.AreNotEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }
        }

        [Test]
        public void TestSendTimerStopEvent ()
        {
            try {
                tracker.SendTimerStopEvent (TimerStopSource.App);
            } catch (Exception e) {
                Assert.AreEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }

            try {
                tracker.SendTimerStopEvent ((TimerStopSource)100);
            } catch (Exception e) {
                Assert.AreNotEqual (TestTracker.SendEventExceptionMessage, e.Message);
            }
        }
    }
}

