using NUnit.Framework;
using Xamarin.UITest;

namespace UITests
{
    public sealed class NoUserTests : TestFixtureBase
    {
        public NoUserTests(Platform platform)
        : base(platform) { }

        [Test]
        public void AppLaunches()
        {
            app.Screenshot("First screen.");
        }

        [Test]
        public void CanLoginWithoutUser()
        {
            loginWithoutUser(false);

            app.Screenshot("Logged in without user");
        }

        [Test]
        public void CanStartTimeEntry()
        {
            setupRunningTimeEntry();

            app.Screenshot("Started time entry");
        }

        [Test]
        public void CanStartAndStopTimeEntry()
        {
            setupRunningTimeEntry();

            app.Tap(ui.MainScreen.StartStopButton);

            app.WaitForNoElement(ui.MainScreen.ActiveTimerData);

            app.Screenshot("Started time entry");
        }

        #region helpers

        private void tapStartStopButton()
        {
            app.WaitForElement(ui.MainScreen.StartStopButton);
            app.Tap(ui.MainScreen.StartStopButton);
        }

        private void closeEditView()
        {
            app.WaitForElement(ui.TimeEntryEditScreen.NavigateUpButton);
            app.Tap(ui.TimeEntryEditScreen.NavigateUpButton);
        }

        private void setupRunningTimeEntry()
        {
            loginWithoutUser();

            tapStartStopButton();

            closeEditView();

            app.WaitForElement(ui.MainScreen.ActiveTimerData);
        }

        private void loginWithoutUser(bool dismissHintOverlay = true)
        {
            // log in without user
            app.WaitForElement(ui.LoginScreen.NoUserStartButton);
            app.Tap(ui.LoginScreen.NoUserStartButton);

            // confirm we are logged in by checking for start-stop button
            app.WaitForElement(ui.MainScreen.StartStopButton);

            if (dismissHintOverlay)
            {
                app.Tap(ui.MainScreen.LayoverButton);
            }
        }

        #endregion
    }
}

