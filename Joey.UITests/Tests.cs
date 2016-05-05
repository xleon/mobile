using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;

namespace Joey.UITests
{
    [TestFixture]
    public class Tests
    {
        AndroidApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            // TODO: If the Android app being tested is included in the solution then open
            // the Unit Tests window, right click Test Apps, select Add App Project
            // and select the app projects that should be tested.
            app = ConfigureApp
                .Android
                // TODO: Update this path to point to your Android app and uncomment the
                // code if the app is not included in the solution.
                //.ApkFile ("../../../Android/bin/Debug/UITestsAndroid.apk")
                .StartApp();
        }

        //[Test]
        //public void AppLaunches()
        //{
        //    app.Repl();
        //}

        private void login()
        {
            // Dismiss "Get Google Play Services" button
            app.Tap(c => c.Marked("button1"));

            // Login
            app.EnterText(c => c.Marked("EmailAutoCompleteTextView"), "a@a.es");
            app.EnterText(c => c.Marked("PasswordEditText"), "123456");
            app.Tap(c => c.Marked("LoginButton"));
        }

        [Test]
        public void LoginAndAddEntry()
        {
            login();

            // Wait for timer screen and check how many elements are in the list
            app.WaitForElement(c => c.Marked("ContinueImageButton"));
            var childCount1 = app.Query(c => c.Marked("LogRecyclerView").Child()).Count();

            // After tapping the Continue button, there should be more elements in the list
            app.Tap(c => c.Marked("ContinueImageButton"));
            var childCount2 = app.Query(c => c.Marked("LogRecyclerView").Child()).Count();
            Assert.Greater(childCount2, childCount1);

            // Stop the new entry
            app.Tap(c => c.Marked("ContinueImageButton"));
            //var childCount3 = app.Query(c => c.Marked("LogRecyclerView").Child()).Count();
            //Assert.AreEqual(childCount3, childCount2);
        }
    }
}

