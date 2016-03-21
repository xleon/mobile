using System;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe._Data;
using Toggl.Phoebe._Data.Models;
using Toggl.Phoebe._Net;
using Toggl.Phoebe._Reactive;
using Toggl.Phoebe._ViewModels;
using Toggl.Phoebe.Analytics;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Reactive
{
    [TestFixture]
    public class LoginVMTest : Test
    {
        NetworkSwitcher networkSwitcher;
        LoginVM viewModel;
        readonly ToggleClientMock togglClient = new ToggleClientMock ();
        readonly PlatformUtils platformUtils = new PlatformUtils();
        ISyncDataStore dataStore;

        public override void Init ()
        {
            base.Init ();

            dataStore = ServiceContainer.Resolve<ISyncDataStore> ();
            ServiceContainer.RegisterScoped<IPlatformUtils> (platformUtils);
            ServiceContainer.RegisterScoped<ITogglClient> (togglClient);
            ServiceContainer.RegisterScoped<ITracker> (new TrackerMock());
            networkSwitcher = new NetworkSwitcher ();
            ServiceContainer.RegisterScoped<Net.INetworkPresence> (networkSwitcher);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            ServiceContainer.RegisterScoped (Moq.Mock.Of<Phoebe.Data.ISettingsStore> ());
            var initState = Util.GetInitAppState ();
            RxChain.Init (initState);

            viewModel = new LoginVM ();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            viewModel.Dispose ();
            dataStore.Table<UserData> ().Delete (e => true);
            RxChain.Cleanup ();
        }

        [Test]
        public void TestLoginEmailPassword ()
        {
            // Set state as connected.
            networkSwitcher.SetNetworkConnection (true);

            viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof (viewModel.AuthResult) &&
                        viewModel.AuthResult != Net.AuthResult.Authenticating) {
                    // Correct login.
                    Assert.That (viewModel.AuthResult, Is.EqualTo (Net.AuthResult.Success));
                    Assert.That (StoreManager.Singleton.AppState.TimerState.User.Email, Is.EqualTo (ToggleClientMock.fakeUserEmail));

                    // Check item has been correctly saved in database
                    Assert.That (dataStore.Table<UserData> ().SingleOrDefault (
                                     x => x.Email == ToggleClientMock.fakeUserEmail), Is.Not.Null);
                }
            };

            // None state.
            Assert.That (viewModel.AuthResult, Is.EqualTo (Net.AuthResult.None));
            viewModel.TryLogin (ToggleClientMock.fakeUserEmail, ToggleClientMock.fakeUserPassword);

            // Authenticating
            Assert.That (viewModel.AuthResult, Is.EqualTo (Net.AuthResult.Authenticating));
        }

        [Test]
        public void TestLoginGoogleToken ()
        {
            // Set state as connected.
            networkSwitcher.SetNetworkConnection (true);

            viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof (viewModel.AuthResult) &&
                        viewModel.AuthResult != Net.AuthResult.Authenticating) {
                    // Correct login.
                    Assert.That (viewModel.AuthResult, Is.EqualTo (Net.AuthResult.Success));
                    Assert.That (StoreManager.Singleton.AppState.TimerState.User.Email, Is.EqualTo (ToggleClientMock.fakeUserEmail));

                    // Check item has been correctly saved in database
                    Assert.That (dataStore.Table<UserData> ().SingleOrDefault (
                                     x => x.Email == ToggleClientMock.fakeUserEmail), Is.Not.Null);
                }
            };

            // None state.
            Assert.That (viewModel.AuthResult, Is.EqualTo (Net.AuthResult.None));
            viewModel.TryLoginWithGoogle (ToggleClientMock.fakeGoogleId);
        }

        [Test]
        public void TestLoginWrongEmailPassword ()
        {
            // Set state as connected.
            networkSwitcher.SetNetworkConnection (true);
            viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof (viewModel.AuthResult) &&
                        viewModel.AuthResult != Net.AuthResult.Authenticating) {
                    // Correct login.
                    Assert.That (viewModel.AuthResult, Is.EqualTo (Net.AuthResult.SystemError));
                    Assert.That (StoreManager.Singleton.AppState.TimerState.User.Email, Is.EqualTo (string.Empty));

                    // Check item has been correctly saved in database
                    Assert.That (dataStore.Table<UserData> ().SingleOrDefault (
                                     x => x.Email == ToggleClientMock.fakeUserEmail), Is.Null);
                }
            };
            viewModel.TryLogin (ToggleClientMock.fakeUserEmail, ToggleClientMock.fakeUserPassword + "_");
        }

        [Test]
        public void TestLoginWrongGoogleToken ()
        {
            // Set state as connected.
            networkSwitcher.SetNetworkConnection (true);
            viewModel.PropertyChanged += (sender, e) => {
                if (e.PropertyName == nameof (viewModel.AuthResult)) {
                    // Not Google account.
                    Assert.That (viewModel.AuthResult, Is.EqualTo (Net.AuthResult.NoGoogleAccount));
                    Assert.That (StoreManager.Singleton.AppState.TimerState.User, Is.Null);

                    // Nothing in DB.
                    Assert.That (dataStore.Table<UserData> ().SingleOrDefault (
                                     x => x.Email == ToggleClientMock.fakeUserEmail), Is.Null);
                }
            };

            // None state.
            viewModel.TryLoginWithGoogle (ToggleClientMock.fakeGoogleId + "__");
        }
    }
}

