using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.iOS;
using Xamarin.UITest.Queries;

namespace Ross.UITests
{
    [TestFixture]
    public class Tests
    {
        iOSApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = ConfigureApp.iOS.StartApp();
        }

        private void login()
        {
            // Tap "Log in" button
            app.Tap(c => c.Marked("Log in"));

            // Login
            app.EnterText(c => c.Marked("Email address"), "a@a.es");
            app.EnterText(c => c.Marked("Password"), "123456");
            app.Tap(c => c.Marked("Log in"));

        }


        [Test]
        public void LoginAndAddEntry()
        {
            login();

            // Wait for timer screen and check how many elements are in the list
            app.WaitForElement(c => c.Marked("Start"));
            var childCount1 = app.Query(c => c.Class("UITableViewWrapperView").Child()).Count();

            // After tapping the Continue button, there should be more elements in the list
            app.Tap(c => c.Marked("Start"));
            app.Back();

            var childCount2 = app.Query(c => c.Class("UITableViewWrapperView").Child()).Count();
            Assert.Greater(childCount2, childCount1);

            // Stop the new entry
            app.Tap(c => c.Marked("Stop"));
        }
    }
}

